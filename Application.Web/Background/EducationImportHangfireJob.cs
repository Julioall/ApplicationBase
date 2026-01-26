using System.Text.Json;
using Application.Domain.Exceptions;
using Application.Domain.Interface;
using Application.Domain.Interface.Education;
using Application.Domain.Model.Education;
using Application.Domain.Model.Education.Dtos;
using Application.Domain.Model.Notification;
using Application.Infrastructure.Service;
using Application.Service.Interface;
using Application.Service.Education;
using Application.Service.Service;
using Application.Shared.Background;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Application.Api.Background;

/// <summary>
/// Job Hangfire para processar importações de educação.
/// Cada execução cria uma nova sessão RavenDB para evitar limite de requisições.
/// </summary>
public class EducationImportHangfireJob : IEducationImportJob
{
    private const int BatchSize = 3; // Reduzido para evitar limite de requisições
    private readonly IEducationImportRepository _importRepository;
    private readonly IEducationRepository _educationRepository;
    private readonly IStringLocalizer<global::Application.Domain.SharedResource> _localizer;
    private readonly IDocumentStore _documentStore;
    private readonly IServiceRavenDB _serviceRavenDb;
    private readonly INotificationService _notificationService;
    private readonly ILogger<EducationImportHangfireJob> _logger;

    public EducationImportHangfireJob(
        IEducationImportRepository importRepository,
        IEducationRepository educationRepository,
        IStringLocalizer<global::Application.Domain.SharedResource> localizer,
        IDocumentStore documentStore,
        IServiceRavenDB serviceRavenDb,
        INotificationService notificationService,
        ILogger<EducationImportHangfireJob> logger)
    {
        _importRepository = importRepository;
        _educationRepository = educationRepository;
        _localizer = localizer;
        _documentStore = documentStore;
        _serviceRavenDb = serviceRavenDb;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Processa um arquivo de importação de educação.
    /// </summary>
    public async Task ProcessImportAsync(string importId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(importId);

        using var asyncSession = _documentStore.OpenAsyncSession();
        using var syncSession = _documentStore.OpenSession();
        asyncSession.Advanced.UseOptimisticConcurrency = false;
        syncSession.Advanced.UseOptimisticConcurrency = false;

        // Reutilizar a mesma sessão nas dependências da infraestrutura
        _serviceRavenDb.Store = _documentStore;
        _serviceRavenDb.AsyncSession = asyncSession;
        _serviceRavenDb.Session = syncSession;

        try
        {
            // Buscar import com sessão separada
            var pending = await _importRepository.GetByIdAsync(importId, cancellationToken);
            if (pending == null)
            {
                _logger.LogWarning("Import {ImportId} not found", importId);
                return;
            }

            if (pending.Status != EducationImportStatus.Pending && pending.Status != EducationImportStatus.Running)
            {
                _logger.LogInformation("Import {ImportId} already processed with status {Status}", importId, pending.Status);
                return;
            }

            // Marcar como em processamento
            pending.Status = EducationImportStatus.Running;
            pending.StartedAt = DateTime.UtcNow;
            await asyncSession.StoreAsync(pending, importId, cancellationToken);
            await asyncSession.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Processing education import {ImportId} from file {FileName}", importId, pending.FileName);

            // Buscar arquivo
            var fileStream = await _importRepository.GetImportFileAsync(importId, cancellationToken);
            if (fileStream == null)
            {
                pending.Status = EducationImportStatus.Failed;
                pending.ErrorMessage = _localizer["CourseImportFileMissing"];
                await asyncSession.StoreAsync(pending, importId, cancellationToken);
                await asyncSession.SaveChangesAsync(cancellationToken);

                await CreateNotificationAsync(pending, NotificationType.Error, cancellationToken);
                return;
            }

            // Processar arquivo em lotes
            await using var buffered = new MemoryStream();
            await fileStream.CopyToAsync(buffered, cancellationToken);
            buffered.Position = 0;

            var result = await ProcessFileInBatchesAsync(buffered, asyncSession, cancellationToken);

            // Atualizar import com resultado
            pending.Processed = result.Processed;
            pending.CreatedUcs = result.CreatedUcs;
            pending.UpdatedUcs = result.UpdatedUcs;
            pending.Linked = result.Linked;
            pending.Errors = result.Errors;
            pending.Status = EducationImportStatus.Completed;
            pending.CompletedAt = DateTime.UtcNow;

            await asyncSession.StoreAsync(pending, importId, cancellationToken);
            await asyncSession.SaveChangesAsync(cancellationToken);

            // Criar notificação de sucesso
            var type = result.Errors.Count > 0 ? NotificationType.Warning : NotificationType.Success;
            var message = _localizer["NotificationImportCompletedMessage", 
                pending.FileName ?? importId, result.CreatedUcs, result.UpdatedUcs, result.Linked];
            
            await _notificationService.CreateAsync(
                _localizer["NotificationImportCompletedTitle"],
                message,
                type,
                importId,
                "education-import",
                "/education",
                cancellationToken);

            _logger.LogInformation("Import {ImportId} completed successfully. Created: {Created}, Updated: {Updated}", 
                importId, result.CreatedUcs, result.UpdatedUcs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing import {ImportId}", importId);

            // Atualizar com erro
            using var errorSession = _documentStore.OpenAsyncSession();
            errorSession.Advanced.UseOptimisticConcurrency = false;
            
            var pending = await errorSession.LoadAsync<EducationImport>(importId, cancellationToken);
            if (pending != null)
            {
                pending.Status = EducationImportStatus.Failed;
                pending.ErrorMessage = ex.Message;
                pending.CompletedAt = DateTime.UtcNow;
                await errorSession.StoreAsync(pending, importId, cancellationToken);
                await errorSession.SaveChangesAsync(cancellationToken);
            }

            await CreateNotificationAsync(
                pending,
                NotificationType.Error,
                cancellationToken);
        }
    }

    private async Task<CourseImportResult> ProcessFileInBatchesAsync(
        Stream fileStream,
        IAsyncDocumentSession asyncSession,
        CancellationToken cancellationToken)
    {
        fileStream.Position = 0;
        JsonDocument document;

        try
        {
            document = await JsonDocument.ParseAsync(fileStream, cancellationToken: cancellationToken);
        }
        catch (Exception)
        {
            throw new BusinessException(_localizer["CourseImportInvalidJson"]);
        }

        using var _ = document;
        var root = document.RootElement;

        if (!TryExtractCourses(root, out var coursesElement))
        {
            throw new BusinessException(_localizer["CourseImportCoursesMissing"]);
        }

        var courses = coursesElement!.Value.EnumerateArray().ToList();
        var result = new CourseImportResult();
        var classPeriods = new Dictionary<string, (long Min, long Max, ClassDocument ClassDoc)>();
        var processor = new EducationImportProcessor(_educationRepository, _localizer);

        // Processar em lotes pequenos com novas sessões a cada lote
        for (var i = 0; i < courses.Count; i += BatchSize)
        {
            var batch = courses.Skip(i).Take(BatchSize);
            await processor.ProcessBatchAsync(batch, classPeriods, result, cancellationToken);

            // Salvar lote e criar nova sessão para evitar limite de requisições
            await asyncSession.SaveChangesAsync(cancellationToken);
            
            _logger.LogDebug("Processed batch {BatchNumber} of {Total}", 
                (i / BatchSize) + 1, (courses.Count + BatchSize - 1) / BatchSize);
        }

        // Atualizar períodos das classes
        foreach (var period in classPeriods.Values)
        {
            if (period.ClassDoc.StartDate != period.Min || period.ClassDoc.EndDate != period.Max)
            {
                period.ClassDoc.StartDate = period.Min;
                period.ClassDoc.EndDate = period.Max;
                await asyncSession.StoreAsync(period.ClassDoc, period.ClassDoc.Id, cancellationToken);
            }
        }

        await asyncSession.SaveChangesAsync(cancellationToken);
        return result;
    }

    private static bool TryExtractCourses(JsonElement root, out JsonElement? coursesElement)
    {
        coursesElement = null;

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (item.TryGetProperty("data", out var dataFromArray) &&
                    dataFromArray.TryGetProperty("courses", out var coursesFromArray) &&
                    coursesFromArray.ValueKind == JsonValueKind.Array)
                {
                    coursesElement = coursesFromArray;
                    return true;
                }
            }
        }
        else if (root.ValueKind == JsonValueKind.Object &&
                 root.TryGetProperty("data", out var dataElement) &&
                 dataElement.TryGetProperty("courses", out var courses) &&
                 courses.ValueKind == JsonValueKind.Array)
        {
            coursesElement = courses;
            return true;
        }

        return false;
    }

    private async Task CreateNotificationAsync(
        EducationImport? import,
        string type,
        CancellationToken cancellationToken)
    {
        if (import == null)
        {
            return;
        }

        try
        {
            var title = type == NotificationType.Error 
                ? _localizer["NotificationImportFailedTitle"]
                : _localizer["NotificationImportCompletedTitle"];

            var message = type == NotificationType.Error
                ? _localizer["NotificationImportFailedMessage", import.FileName ?? import.Id, import.ErrorMessage ?? string.Empty]
                : _localizer["NotificationImportCompletedMessage", import.FileName ?? import.Id, import.CreatedUcs, import.UpdatedUcs, import.Linked];

            await _notificationService.CreateAsync(
                title,
                message,
                type,
                import.Id,
                "education-import",
                "/education",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification for import {ImportId}", import?.Id);
        }
    }
}
