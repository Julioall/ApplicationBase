using Application.Domain.Interface.Education;
using Application.Domain.Model.Education;
using Application.Domain.Model.Notification;
using Application.Service.Interface;
using Application.Shared.Background;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Application.Api.Background
{
    public class EducationReportImportHangfireJob : IEducationReportImportJob
    {
        private readonly IEducationReportImportRepository _reportImportRepository;
        private readonly IEducationService _educationService;
        private readonly INotificationService _notificationService;
        private readonly IStringLocalizer<global::Application.Domain.SharedResource> _localizer;
        private readonly IDocumentStore _documentStore;
        private readonly Application.Infrastructure.Service.IServiceRavenDB _serviceRavenDb;
        private readonly ILogger<EducationReportImportHangfireJob> _logger;

        public EducationReportImportHangfireJob(
            IEducationReportImportRepository reportImportRepository,
            IEducationService educationService,
            INotificationService notificationService,
            IStringLocalizer<global::Application.Domain.SharedResource> localizer,
            IDocumentStore documentStore,
            Application.Infrastructure.Service.IServiceRavenDB serviceRavenDb,
            ILogger<EducationReportImportHangfireJob> logger)
        {
            _reportImportRepository = reportImportRepository;
            _educationService = educationService;
            _notificationService = notificationService;
            _localizer = localizer;
            _documentStore = documentStore;
            _serviceRavenDb = serviceRavenDb;
            _logger = logger;
        }

        public async Task ProcessReportAsync(string importId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(importId);

            var sessionOptions = new Raven.Client.Documents.Session.SessionOptions
            {
                NoTracking = false
            };

            using var asyncSession = _documentStore.OpenAsyncSession(sessionOptions);
            using var syncSession = _documentStore.OpenSession(sessionOptions);
            asyncSession.Advanced.UseOptimisticConcurrency = false;
            syncSession.Advanced.UseOptimisticConcurrency = false;

            _serviceRavenDb.Store = _documentStore;
            _serviceRavenDb.AsyncSession = asyncSession;
            _serviceRavenDb.Session = syncSession;

            try
            {
                var import = await _reportImportRepository.GetByIdAsync(importId, cancellationToken);
                if (import == null)
                {
                    _logger.LogWarning("EducationReportImportHangfireJob: import {ImportId} not found", importId);
                    return;
                }

                if (import.Status != EducationImportStatus.Pending && import.Status != EducationImportStatus.Running)
                {
                    _logger.LogInformation("EducationReportImportHangfireJob: import {ImportId} already processed with status {Status}", importId, import.Status);
                    return;
                }

                import.Status = EducationImportStatus.Running;
                import.StartedAt = DateTime.UtcNow;
                await _reportImportRepository.UpdateAsync(import, cancellationToken);
                await asyncSession.SaveChangesAsync(cancellationToken);

                var files = await _reportImportRepository.GetImportFilesAsync(importId, cancellationToken);
                var result = await _educationService.ImportReportAsync(files, cancellationToken);

                import.Result = result;
                import.Status = EducationImportStatus.Completed;
                import.CompletedAt = DateTime.UtcNow;
                import.ErrorMessage = null;

                await _reportImportRepository.UpdateAsync(import, cancellationToken);
                await asyncSession.SaveChangesAsync(cancellationToken);

                await CreateNotificationAsync(
                    NotificationType.Success,
                    _localizer["NotificationReportImportCompletedTitle"],
                    _localizer["NotificationReportImportCompletedMessage", result.RowsRead, result.PerformanceRecordsUpserted],
                    importId,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EducationReportImportHangfireJob: error processing report import {ImportId}", importId);

                await MarkFailedAsync(importId, ex.Message, cancellationToken);

                await CreateNotificationAsync(
                    NotificationType.Error,
                    _localizer["NotificationReportImportFailedTitle"],
                    _localizer["NotificationReportImportFailedMessage", importId, ex.Message],
                    importId,
                    cancellationToken);
            }
        }

        private async Task MarkFailedAsync(string importId, string? errorMessage, CancellationToken cancellationToken)
        {
            var import = await _reportImportRepository.GetByIdAsync(importId, cancellationToken)
                         ?? new EducationReportImport { Id = importId };

            import.Status = EducationImportStatus.Failed;
            import.ErrorMessage = errorMessage;
            import.CompletedAt = DateTime.UtcNow;

            await _reportImportRepository.UpdateAsync(import, cancellationToken);
            await _serviceRavenDb.AsyncSession.SaveChangesAsync(cancellationToken);
        }

        private Task CreateNotificationAsync(string type, string title, string message, string referenceId, CancellationToken cancellationToken)
        {
            return _notificationService.CreateAsync(
                title,
                message,
                type,
                referenceId,
                "education-report-import",
                "/education",
                cancellationToken);
        }
    }
}
