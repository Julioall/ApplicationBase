using Application.Domain.Interface.Education;
using Application.Domain.Model.Education;
using Application.Infrastructure.Service;
using Application.Shared.Background;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;

namespace Application.Api.Background
{
    public class EducationSyncHangfireJob : IEducationSyncJob
    {
        private readonly Shared.Session.ISessionTokenCache _sessionTokenCache;
        private readonly IEducationRepository _educationRepository;
        private readonly IServiceRavenDB _serviceRavenDb;
        private readonly IDocumentStore _documentStore;
        private readonly IStringLocalizer<Domain.SharedResource> _localizer;
        private readonly ILogger<EducationSyncHangfireJob> _logger;
        private readonly Service.Interface.IMoodleAuthClient _moodleAuthClient;
        private readonly Service.Interface.IMoodleCourseClient _moodleCourseClient;
        private const string SyncStatusRunning = "Running";
        private const string SyncStatusSuccess = "Success";
        private const string SyncStatusFailed = "Failed";
        private const int SyncTtlSeconds = 24 * 60 * 60;

        public EducationSyncHangfireJob(
            IEducationRepository educationRepository,
            IServiceRavenDB serviceRavenDb,
            IDocumentStore documentStore,
            IStringLocalizer<Domain.SharedResource> localizer,
            ILogger<EducationSyncHangfireJob> logger,
            Service.Interface.IMoodleAuthClient moodleAuthClient,
            Service.Interface.IMoodleCourseClient moodleCourseClient,
            Shared.Session.ISessionTokenCache sessionTokenCache)
        {
            _educationRepository = educationRepository;
            _serviceRavenDb = serviceRavenDb;
            _documentStore = documentStore;
            _localizer = localizer;
            _logger = logger;
            _moodleAuthClient = moodleAuthClient;
            _moodleCourseClient = moodleCourseClient;
            _sessionTokenCache = sessionTokenCache;
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
            status.Message = _localizer["EducationSyncRunning"];
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
                        throw new InvalidOperationException("O triggeredByUserId deve ser o id numérico do usuário Moodle para sincronização.");
                    }
                }

                // Exemplo: obter token do usuário (ajuste conforme sua estratégia de autenticação)
                // Aqui, para simplificação, buscamos o token via _moodleAuthClient.GetSiteInfoAsync, mas normalmente o token viria do contexto do usuário logado.
                // Adapte para obter o token real do usuário logado.
                var token = await ObterTokenUsuarioAsync(moodleUserId, cancellationToken);
                if (string.IsNullOrWhiteSpace(token))
                    throw new InvalidOperationException("Token do usuário Moodle não encontrado para sincronização.");

                // 2. Buscar cursos do usuário logado
                var cursos = await _moodleCourseClient.GetUserCoursesAsync(moodleUserId, token, cancellationToken);

                // 3. Mapear e salvar no repositório (exemplo: cada curso vira um UcDocument)
                foreach (var curso in cursos)
                {
                    var uc = new UcDocument
                    {
                        EadId = curso.Id,
                        Fullname = curso.FullName ?? curso.DisplayName ?? string.Empty,
                        StartDate = curso.StartDate ?? 0,
                        EndDate = curso.EndDate ?? 0,
                        ViewUrl = $"/course/view.php?id={curso.Id}",
                        CourseImage = !string.IsNullOrWhiteSpace(curso.CourseImage)
                            ? curso.CourseImage
                            : (curso.OverviewFiles != null && curso.OverviewFiles.Count > 0 ? curso.OverviewFiles[0].FileUrl : null),
                        CourseCategory = curso.CategoryId.ToString(),
                        SchoolNameDerived = null, // Para preencher, buscar nome da categoria via endpoint extra
                        ProgramNameDerived = curso.FullName ?? curso.DisplayName ?? string.Empty,
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
                    await _educationRepository.UpsertUcAsync(uc, cancellationToken);
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
                status.Message = _localizer["EducationSyncCompleted"];

                await _educationRepository.UpdateSyncStatusAsync(status, cancellationToken);
                await asyncSession.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Education sync completed by trigger {UserId}", triggeredByUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Education sync failed for trigger {UserId}", triggeredByUserId);

                status.Status = SyncStatusFailed;
                status.Message = _localizer["EducationSyncFailed"];

                await _educationRepository.UpdateSyncStatusAsync(status, cancellationToken);
                await asyncSession.SaveChangesAsync(cancellationToken);

                throw;
            }

        }

        // Método auxiliar para obter token do usuário logado (ajuste conforme sua estratégia de autenticação)
        private async Task<string?> ObterTokenUsuarioAsync(int moodleUserId, CancellationToken cancellationToken)
        {
            // Buscar token do cache de sessão
            return _sessionTokenCache.GetMoodleToken(moodleUserId);
        }
    }
}
