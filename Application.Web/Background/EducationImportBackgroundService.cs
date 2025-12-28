using System.Text.Json;
using System.Linq;
using Application.Domain.Interface.Education;
using Application.Domain.Model.Education;
using Application.Domain.Model.Education.Dtos;
using Application.Domain.Model.Notification;
using Application.Infrastructure.Service;
using Application.Service.Interface;
using Application.Service.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Application.Domain.Exceptions;

namespace Application.Api.Background
{
    public class EducationImportBackgroundService : BackgroundService
    {
        private const int BatchSize = 5;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EducationImportBackgroundService> _logger;

        public EducationImportBackgroundService(IServiceScopeFactory scopeFactory, ILogger<EducationImportBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessNextImportAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process education import.");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        private async Task ProcessNextImportAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var importRepository = scope.ServiceProvider.GetRequiredService<IEducationImportRepository>();
            var educationRepository = scope.ServiceProvider.GetRequiredService<IEducationRepository>();
            var localizer = scope.ServiceProvider.GetRequiredService<IStringLocalizer<Application.Domain.SharedResource>>();
            var serviceRaven = scope.ServiceProvider.GetRequiredService<IServiceRavenDB>();
            var documentStore = scope.ServiceProvider.GetRequiredService<IDocumentStore>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            serviceRaven.Store = documentStore;
            serviceRaven.AsyncSession = documentStore.OpenAsyncSession();
            serviceRaven.Session = documentStore.OpenSession();
            serviceRaven.Session.Advanced.UseOptimisticConcurrency = false;
            serviceRaven.AsyncSession.Advanced.UseOptimisticConcurrency = false;

            var pending = await importRepository.GetNextPendingAsync(cancellationToken);
            if (pending == null)
            {
                return;
            }

            pending.Status = EducationImportStatus.Running;
            pending.StartedAt = DateTime.UtcNow;
            await importRepository.UpdateAsync(pending, cancellationToken);
            await serviceRaven.AsyncSession.SaveChangesAsync(cancellationToken);

            var fileStream = await importRepository.GetImportFileAsync(pending.Id!, cancellationToken);
            if (fileStream == null)
            {
                pending.Status = EducationImportStatus.Failed;
                pending.ErrorMessage = localizer["CourseImportFileMissing"];
                await importRepository.UpdateAsync(pending, cancellationToken);
                await notificationService.CreateAsync(
                    localizer["NotificationImportFailedTitle"],
                    localizer["NotificationImportFailedMessage", pending.FileName ?? pending.Id, pending.ErrorMessage ?? string.Empty],
                    NotificationType.Error,
                    pending.Id,
                    "education-import",
                    "/education",
                    cancellationToken);
                await serviceRaven.AsyncSession.SaveChangesAsync(cancellationToken);
                return;
            }

            try
            {
                await using var buffered = new MemoryStream();
                await fileStream.CopyToAsync(buffered, cancellationToken);
                buffered.Position = 0;

                var result = await ProcessFileInBatchesAsync(buffered, educationRepository, localizer, documentStore, serviceRaven, cancellationToken);
                pending.Processed = result.Processed;
                pending.CreatedUcs = result.CreatedUcs;
                pending.UpdatedUcs = result.UpdatedUcs;
                pending.Linked = result.Linked;
                pending.Errors = result.Errors;
                pending.Status = EducationImportStatus.Completed;
                pending.CompletedAt = DateTime.UtcNow;

                var type = result.Errors.Count > 0 ? NotificationType.Warning : NotificationType.Success;
                var message = localizer["NotificationImportCompletedMessage", pending.FileName ?? pending.Id, result.CreatedUcs, result.UpdatedUcs, result.Linked];
                await notificationService.CreateAsync(
                    localizer["NotificationImportCompletedTitle"],
                    message,
                    type,
                    pending.Id,
                    "education-import",
                    "/education",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Import {ImportId} failed", pending.Id);
                pending.Status = EducationImportStatus.Failed;
                pending.ErrorMessage = ex.Message;
                await notificationService.CreateAsync(
                    localizer["NotificationImportFailedTitle"],
                    localizer["NotificationImportFailedMessage", pending.FileName ?? pending.Id, ex.Message],
                    NotificationType.Error,
                    pending.Id,
                    "education-import",
                    "/education",
                    cancellationToken);
            }

            await importRepository.UpdateAsync(pending, cancellationToken);
            await serviceRaven.AsyncSession.SaveChangesAsync(cancellationToken);
        }

        private async Task<CourseImportResult> ProcessFileInBatchesAsync(Stream fileStream, IEducationRepository educationRepository, IStringLocalizer<Application.Domain.SharedResource> localizer, IDocumentStore store, IServiceRavenDB serviceRaven, CancellationToken cancellationToken)
        {
            fileStream.Position = 0;
            JsonDocument document;
            try
            {
                document = await JsonDocument.ParseAsync(fileStream, cancellationToken: cancellationToken);
            }
            catch (Exception)
            {
                throw new BusinessException(localizer["CourseImportInvalidJson"]);
            }

            using var _ = document;
            var root = document.RootElement;
            if (!TryExtractCourses(root, out var coursesElement))
            {
                throw new BusinessException(localizer["CourseImportCoursesMissing"]);
            }

            var courses = coursesElement!.Value.EnumerateArray().ToList();
            var result = new CourseImportResult();
            var classPeriods = new Dictionary<string, (long Min, long Max, ClassDocument ClassDoc)>();
            var processor = new EducationImportProcessor(educationRepository, localizer);

            for (var i = 0; i < courses.Count; i += BatchSize)
            {
                var batch = courses.Skip(i).Take(BatchSize);
                await processor.ProcessBatchAsync(batch, classPeriods, result, cancellationToken);

                await serviceRaven.AsyncSession.SaveChangesAsync(cancellationToken);
                serviceRaven.Session?.Dispose();
                serviceRaven.AsyncSession?.Dispose();
                serviceRaven.Session = store.OpenSession();
                serviceRaven.AsyncSession = store.OpenAsyncSession();
                serviceRaven.Session.Advanced.UseOptimisticConcurrency = false;
                serviceRaven.AsyncSession.Advanced.UseOptimisticConcurrency = false;
            }

            foreach (var period in classPeriods.Values)
            {
                if (period.ClassDoc.StartDate != period.Min || period.ClassDoc.EndDate != period.Max)
                {
                    period.ClassDoc.StartDate = period.Min;
                    period.ClassDoc.EndDate = period.Max;
                    await serviceRaven.AsyncSession.StoreAsync(period.ClassDoc, period.ClassDoc.Id, cancellationToken);
                }
            }

            await serviceRaven.AsyncSession.SaveChangesAsync(cancellationToken);
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
    }
}
