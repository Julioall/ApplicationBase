using Application.Domain.Interface.Education;
using Application.Domain.Interface.Moodle;
using Application.Domain.Model;
using Application.Domain.Model.Education;
using Application.Infrastructure.Service;
using Application.Shared.Background;
using Application.Domain.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;

namespace Application.Api.Background
{
    public class MoodleSyncHangfireJob : IMoodleSyncJob
    {
        private readonly Shared.Session.ISessionTokenCache _sessionTokenCache;
        private readonly IEducationRepository _educationRepository;
        private readonly IMoodleRepository _moodleRepository;
        private readonly IServiceRavenDB _serviceRavenDb;
        private readonly IDocumentStore _documentStore;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ILogger<MoodleSyncHangfireJob> _logger;
        private readonly Service.Interface.IMoodleAuthClient _moodleAuthClient;
        private readonly Service.Interface.IMoodleCourseClient _moodleCourseClient;
        private readonly MoodleSettings _moodleSettings;
        private const string SyncStatusRunning = "Running";
        private const string SyncStatusSuccess = "Success";
        private const string SyncStatusFailed = "Failed";
        private const int SyncTtlSeconds = 24 * 60 * 60;

        public MoodleSyncHangfireJob(
            IEducationRepository educationRepository,
            IMoodleRepository moodleRepository,
            IServiceRavenDB serviceRavenDb,
            IDocumentStore documentStore,
            IStringLocalizer<SharedResource> localizer,
            ILogger<MoodleSyncHangfireJob> logger,
            Service.Interface.IMoodleAuthClient moodleAuthClient,
            Service.Interface.IMoodleCourseClient moodleCourseClient,
            Shared.Session.ISessionTokenCache sessionTokenCache,
            IOptions<MoodleSettings> moodleSettings)
        {
            _educationRepository = educationRepository;
            _moodleRepository = moodleRepository;
            _serviceRavenDb = serviceRavenDb;
            _documentStore = documentStore;
            _localizer = localizer;
            _logger = logger;
            _moodleAuthClient = moodleAuthClient;
            _moodleCourseClient = moodleCourseClient;
            _sessionTokenCache = sessionTokenCache;
            _moodleSettings = moodleSettings.Value;
        }

        public async Task RunAsync(string triggeredByUserId, string triggeredByName, CancellationToken cancellationToken = default)
        {
            using var asyncSession = _documentStore.OpenAsyncSession();
            using var syncSession = _documentStore.OpenSession();
            asyncSession.Advanced.UseOptimisticConcurrency = false;
            syncSession.Advanced.UseOptimisticConcurrency = false;

            _serviceRavenDb.Store = _documentStore;
            _serviceRavenDb.AsyncSession = asyncSession;
            _serviceRavenDb.Session = syncSession;

            var status = await _educationRepository.GetSyncStatusAsync(cancellationToken);
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            status.Status = SyncStatusRunning;
            status.Message = _localizer["MoodleSyncRunning"];
            status.TriggeredByUserId = string.IsNullOrWhiteSpace(triggeredByUserId) ? status.TriggeredByUserId : triggeredByUserId;
            status.TriggeredByName = string.IsNullOrWhiteSpace(triggeredByName) ? status.TriggeredByName : triggeredByName;
            status.TriggeredAt ??= now;

            await _educationRepository.UpdateSyncStatusAsync(status, cancellationToken);
            await asyncSession.SaveChangesAsync(cancellationToken);

            try
            {
                // 1. Obter token e dados do usuário logado (via triggeredByUserId)
                // Permitir fallback para formato 'moodle:245956' ou apenas o número
                int moodleUserId;
                if (!int.TryParse(triggeredByUserId, out moodleUserId))
                {
                    // Tentar extrair número do formato 'moodle:245956'
                    var parts = triggeredByUserId?.Split(':');
                    if (parts?.Length == 2 && int.TryParse(parts[1], out moodleUserId))
                    {
                        // ok
                    }
                    else
                    {
                        status.Status = SyncStatusFailed;
                        status.Message = _localizer["MoodleSyncInvalidUserId"];
                        await _educationRepository.UpdateSyncStatusAsync(status, cancellationToken);
                        await asyncSession.SaveChangesAsync(cancellationToken);
                        _logger.LogWarning("Moodle sync failed: triggeredByUserId must be a numeric Moodle user id. Got: {UserId}", triggeredByUserId);
                        return;
                    }
                }

                // Exemplo: obter token do usuário (ajuste conforme sua estratégia de autenticação)
                // Aqui, para simplificação, buscamos o token via _moodleAuthClient.GetSiteInfoAsync, mas normalmente o token viria do contexto do usuário logado.
                // Adapte para obter o token real do usuário logado.
                var token = await ObterTokenUsuarioAsync(moodleUserId, cancellationToken);
                if (string.IsNullOrWhiteSpace(token))
                {
                    status.Status = SyncStatusFailed;
                    status.Message = _localizer["MoodleSyncMissingToken"];
                    await _educationRepository.UpdateSyncStatusAsync(status, cancellationToken);
                    await asyncSession.SaveChangesAsync(cancellationToken);
                    _logger.LogWarning("Moodle sync failed: Moodle token not found for user {UserId}. Configure MOODLESETTINGS__FIXEDTOKEN or ensure user is authenticated.", moodleUserId);
                    return;
                }

                // 2. Buscar cursos do usuário logado
                var cursos = await _moodleCourseClient.GetUserCoursesAsync(moodleUserId, token, cancellationToken);

                // 2.1. Buscar categorias localmente primeiro (estratégia de cache local)
                var categoryIds = cursos
                    .Where(c => c.CategoryId > 0)
                    .Select(c => c.CategoryId)
                    .Distinct()
                    .ToList();

                // Buscar categorias já armazenadas localmente (convertendo para dicionário mutável)
                var localCategoriesReadOnly = categoryIds.Count > 0
                    ? await _moodleRepository.GetCategoriesByMoodleIdsAsync(categoryIds, cancellationToken)
                    : new Dictionary<int, Domain.Model.Moodle.MoodleCategory>();

                var localCategories = new Dictionary<int, Domain.Model.Moodle.MoodleCategory>(localCategoriesReadOnly);

                // Identificar categorias que ainda não temos localmente
                var missingCategoryIds = categoryIds.Where(id => !localCategories.ContainsKey(id)).ToList();

                // Buscar do Moodle apenas as categorias que faltam e salvar localmente (batch operation)
                if (missingCategoryIds.Count > 0)
                {
                    _logger.LogInformation("Fetching {Count} missing categories from Moodle", missingCategoryIds.Count);
                    var moodleCategories = await _moodleCourseClient.GetCategoriesAsync(missingCategoryIds, token, cancellationToken);

                    if (moodleCategories.Count > 0)
                    {
                        // Batch upsert all missing categories in a single operation
                        _logger.LogInformation("Upserting {Count} categories in batch", moodleCategories.Count);
                        var savedCategories = await _moodleRepository.UpsertCategoriesBatchAsync(moodleCategories.Values, cancellationToken);

                        foreach (var kvp in savedCategories)
                        {
                            localCategories[kvp.Key] = kvp.Value;
                        }
                    }
                }

                // 2.2. Coletar todos os IDs de categorias da hierarquia (via Path) que precisamos
                var allHierarchyIds = new HashSet<int>();
                foreach (var cat in localCategories.Values)
                {
                    if (!string.IsNullOrWhiteSpace(cat.Path))
                    {
                        var pathIds = cat.Path.Split('/', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => int.TryParse(s, out var id) ? id : 0)
                            .Where(id => id > 0);
                        foreach (var id in pathIds)
                        {
                            allHierarchyIds.Add(id);
                        }
                    }
                }

                // Identificar categorias da hierarquia que ainda não temos
                var missingHierarchyIds = allHierarchyIds.Where(id => !localCategories.ContainsKey(id)).ToList();
                if (missingHierarchyIds.Count > 0)
                {
                    _logger.LogInformation("Fetching {Count} hierarchy categories from Moodle", missingHierarchyIds.Count);
                    var hierarchyCategories = await _moodleCourseClient.GetCategoriesAsync(missingHierarchyIds, token, cancellationToken);

                    if (hierarchyCategories.Count > 0)
                    {
                        _logger.LogInformation("Upserting {Count} hierarchy categories in batch", hierarchyCategories.Count);
                        var savedHierarchy = await _moodleRepository.UpsertCategoriesBatchAsync(hierarchyCategories.Values, cancellationToken);

                        foreach (var kvp in savedHierarchy)
                        {
                            localCategories[kvp.Key] = kvp.Value;
                        }
                    }
                }

                // 3. Mapear cursos para CourseUnits (batch processing para evitar limite de requisições do RavenDB)
                var courseUnitsToUpsert = new List<CourseUnit>();
                foreach (var curso in cursos)
                {
                    // Extrair a hierarquia completa da categoria
                    string? institutionName = null, schoolName = null, courseName = null, eventName = null;
                    int? institutionMoodleId = null, schoolMoodleId = null, courseMoodleId = null, eventMoodleId = null;

                    if (curso.CategoryId > 0 && localCategories.TryGetValue(curso.CategoryId, out var categoryInfo))
                    {
                        // Parsear o path para extrair a hierarquia
                        // Path exemplo: /84/87/6375/6406 → Instituição(1)/Escola(2)/Curso(3)/Turma(4)
                        if (!string.IsNullOrWhiteSpace(categoryInfo.Path))
                        {
                            var pathIds = categoryInfo.Path.Split('/', StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => int.TryParse(s, out var id) ? id : 0)
                                .Where(id => id > 0)
                                .ToList();

                            // Mapear por posição (depth)
                            for (int i = 0; i < pathIds.Count; i++)
                            {
                                var catId = pathIds[i];
                                if (localCategories.TryGetValue(catId, out var cat))
                                {
                                    switch (i)
                                    {
                                        case 0: // Depth 1 - Instituição
                                            institutionName = cat.Name;
                                            institutionMoodleId = cat.MoodleId;
                                            break;
                                        case 1: // Depth 2 - Escola
                                            schoolName = cat.Name;
                                            schoolMoodleId = cat.MoodleId;
                                            break;
                                        case 2: // Depth 3 - Curso
                                            courseName = cat.Name;
                                            courseMoodleId = cat.MoodleId;
                                            break;
                                        case 3: // Depth 4 - Turma/Evento
                                            eventName = cat.Name;
                                            eventMoodleId = cat.MoodleId;
                                            break;
                                    }
                                }
                            }
                        }
                    }

                    var courseUnit = new CourseUnit
                    {
                        MoodleId = curso.Id,
                        Fullname = curso.FullName ?? curso.DisplayName ?? string.Empty,
                        StartDate = curso.StartDate ?? 0,
                        EndDate = curso.EndDate ?? 0,
                        ViewUrl = $"/course/view.php?id={curso.Id}",
                        CourseImage = !string.IsNullOrWhiteSpace(curso.CourseImage)
                            ? curso.CourseImage
                            : (curso.OverviewFiles != null && curso.OverviewFiles.Count > 0 ? curso.OverviewFiles[0].FileUrl : null),
                        CourseCategory = curso.CategoryId > 0 ? curso.CategoryId.ToString() : null,

                        // Hierarquia completa de categorias
                        InstitutionName = institutionName,
                        InstitutionMoodleId = institutionMoodleId,
                        SchoolName = schoolName,
                        SchoolMoodleId = schoolMoodleId,
                        CourseName = courseName,
                        CourseMoodleId = courseMoodleId,
                        EventName = eventName,
                        EventMoodleId = eventMoodleId,

                        PeriodTextDerived = (curso.StartDate.HasValue && curso.EndDate.HasValue)
                            ? $"{DateTimeOffset.FromUnixTimeSeconds(curso.StartDate.Value).UtcDateTime:dd/MM/yyyy} a {DateTimeOffset.FromUnixTimeSeconds(curso.EndDate.Value).UtcDateTime:dd/MM/yyyy}"
                            : null,
                        Progress = curso.Progress,
                        Completed = curso.Completed,
                        IsFavourite = curso.IsFavourite,
                        Hidden = curso.Hidden,
                        Summary = curso.Summary,
                        LastAccess = curso.LastAccess,
                        IdNumber = curso.IdNumber,
                        Lang = curso.Lang
                    };
                    courseUnitsToUpsert.Add(courseUnit);
                }

                // Upsert todos os cursos em uma única operação batch
                List<Application.Domain.Model.Education.Dtos.CourseUnitUpsertResult> upsertResults = new();
                if (courseUnitsToUpsert.Count > 0)
                {
                    _logger.LogInformation("Upserting {Count} courses in batch", courseUnitsToUpsert.Count);
                    var results = await _educationRepository.UpsertCourseUnitBatchAsync(courseUnitsToUpsert, cancellationToken);
                    upsertResults = results.ToList();
                }

                // Criar mapeamentos usuário->curso em batch (se usuário app existir)
                try
                {
                    // Resolve usuário interno pelo MoodleId (armazena apenas o número normalmente)
                    var moodleUserKey = moodleUserId.ToString();
                    var appUser = await _serviceRavenDb.AsyncSession.Query<Application.Domain.Model.User.User>()
                        .Where(u => u.Account.MoodleId == moodleUserKey)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (appUser == null)
                    {
                        // Tentar com prefixo 'moodle:' caso o MoodleId tenha sido salvo assim
                        var prefixed = $"moodle:{moodleUserKey}";
                        appUser = await _serviceRavenDb.AsyncSession.Query<Application.Domain.Model.User.User>()
                            .Where(u => u.Account.MoodleId == prefixed)
                            .FirstOrDefaultAsync(cancellationToken);
                    }

                    if (appUser != null && upsertResults.Count > 0)
                    {
                        // Construir mapas com os ids resultantes do upsert (Entity.Id)
                        var maps = upsertResults
                            .Where(r => r.Entity != null && !string.IsNullOrWhiteSpace(r.Entity.Id))
                            .Select(r => new Application.Domain.Model.Moodle.StudentCourseMap
                            {
                                StudentId = appUser.Id,
                                CourseId = r.Entity.Id
                            })
                            .ToList();

                        if (maps.Count > 0)
                        {
                            var batchSize = _moodleSettings.SyncBatchSize > 0 ? _moodleSettings.SyncBatchSize : 50;
                            for (int i = 0; i < maps.Count; i += batchSize)
                            {
                                var batch = maps.Skip(i).Take(batchSize);
                                await _moodleRepository.EnsureStudentCourseMapBatchAsync(batch, cancellationToken);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create student-course maps during Moodle sync for user {UserId}", triggeredByUserId);
                }


                // 4. Recalcular períodos se necessário (exemplo: para cada curso)
                // foreach (var curso in cursos)
                // {
                //     await _educationRepository.RecalculateClassPeriodAsync(..., cancellationToken);
                // }

                var finishedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                status.Status = SyncStatusSuccess;
                status.LastSyncAt = finishedAt;
                status.ExpiresAt = finishedAt + SyncTtlSeconds;
                status.Message = _localizer["MoodleSyncCompleted"];

                await _educationRepository.UpdateSyncStatusAsync(status, cancellationToken);
                await asyncSession.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Moodle sync completed by trigger {UserId}", triggeredByUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Moodle sync failed for trigger {UserId}", triggeredByUserId);

                status.Status = SyncStatusFailed;
                status.Message = _localizer["MoodleSyncFailed"];

                await _educationRepository.UpdateSyncStatusAsync(status, cancellationToken);
                await asyncSession.SaveChangesAsync(cancellationToken);

                throw;
            }

        }

        // Método auxiliar para obter token do usuário logado (ajuste conforme sua estratégia de autenticação)
        private Task<string?> ObterTokenUsuarioAsync(int moodleUserId, CancellationToken cancellationToken)
        {
            // Primeiro tenta o cache de sessão
            var cachedToken = _sessionTokenCache.GetMoodleToken(moodleUserId);
            if (!string.IsNullOrWhiteSpace(cachedToken))
            {
                return Task.FromResult<string?>(cachedToken);
            }

            // Fallback para o token fixo configurado (para jobs em background)
            if (!string.IsNullOrWhiteSpace(_moodleSettings.FixedToken))
            {
                _logger.LogDebug("Using fixed Moodle token for background sync");
                return Task.FromResult<string?>(_moodleSettings.FixedToken);
            }

            return Task.FromResult<string?>(null);
        }
    }
}
