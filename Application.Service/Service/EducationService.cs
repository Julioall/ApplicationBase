using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Application.Domain;
using Application.Domain.Exceptions;
using Application.Domain.Interface;
using Application.Domain.Interface.Education;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.Education;
using Application.Domain.Model.Education.Dtos;
using Application.Domain.Model.Students;
using Application.Service.Interface;
using Application.Shared.Background;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Application.Service.Service
{
    public class EducationService : IEducationService
    {
        private readonly IEducationRepository _educationRepository;
        private readonly IStudentUcPerformanceRepository _studentUcPerformanceRepository;
        private readonly IBackgroundJobScheduler _backgroundJobScheduler;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ILogger<EducationService> _logger;
        private const string SyncStatusRunning = "Running";
        private const string SyncStatusSuccess = "Success";
        private const string SyncStatusFailed = "Failed";
        private const int ManualSyncCooldownSeconds = 300;

        public EducationService(
            IEducationRepository educationRepository, 
            IStudentUcPerformanceRepository studentUcPerformanceRepository,
            IBackgroundJobScheduler backgroundJobScheduler,
            IStringLocalizer<SharedResource> localizer,
            ILogger<EducationService> logger)
        {
            _educationRepository = educationRepository;
            _studentUcPerformanceRepository = studentUcPerformanceRepository;
            _backgroundJobScheduler = backgroundJobScheduler;
            _localizer = localizer;
            _logger = logger;
        }

        public Task<IReadOnlyCollection<School>> GetSchoolsAsync(CancellationToken cancellationToken = default)
        {
            return _educationRepository.GetSchoolsAsync(cancellationToken);
        }

        public Task<IReadOnlyCollection<ProgramDocument>> GetProgramsBySchoolAsync(string schoolId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(schoolId);
            return _educationRepository.GetProgramsBySchoolAsync(schoolId, cancellationToken);
        }

        public Task<IReadOnlyCollection<ClassDocument>> GetClassesByProgramAsync(string programId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(programId);
            return _educationRepository.GetClassesByProgramAsync(programId, cancellationToken);
        }

        public Task<IReadOnlyCollection<UcDocument>> GetUcsByClassAsync(string classId, string? search = null, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(classId);
            return _educationRepository.GetUcsByClassAsync(classId, search, cancellationToken);
        }

        public Task<PagedResult<UcDocument>> SearchUcsAsync(PaginationQuery query, string? classId = null, string? programId = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);
            return _educationRepository.SearchUcsAsync(query.Search, classId, programId, query, cancellationToken);
        }

        public Task<EducationSyncStatus> GetSyncStatusAsync(CancellationToken cancellationToken = default)
        {
            return _educationRepository.GetSyncStatusAsync(cancellationToken);
        }

        public async Task<EducationSyncStatus> TriggerSyncAsync(string userId, string? userName, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);

            var status = await _educationRepository.GetSyncStatusAsync(cancellationToken);
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (string.Equals(status.Status, SyncStatusRunning, StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException(_localizer["EducationSyncAlreadyRunning"]);
            }

            if (status.TriggeredAt.HasValue && now - status.TriggeredAt.Value < ManualSyncCooldownSeconds)
            {
                var minutes = (int)Math.Ceiling(ManualSyncCooldownSeconds / 60.0);
                throw new TooManyRequestsException(_localizer["EducationSyncRateLimited", minutes]);
            }

            status.Status = SyncStatusRunning;
            status.Message = _localizer["EducationSyncQueued"];
            status.TriggeredAt = now;
            status.TriggeredByUserId = userId;
            status.TriggeredByName = string.IsNullOrWhiteSpace(userName) ? null : userName;

            await _educationRepository.UpdateSyncStatusAsync(status, cancellationToken);

            // Enqueue the background job
            var triggeredByUserId = userId;
            var triggeredByName = userName ?? string.Empty;
            _backgroundJobScheduler.Enqueue<IMoodleSyncJob>(job => job.RunAsync(triggeredByUserId, triggeredByName, CancellationToken.None));

            _logger.LogInformation("Moodle manual sync triggered and queued by {UserId}", userId);

            return status;
        }

        public async Task<IReadOnlyCollection<Student>> GetStudentsByUcEadIdAsync(int eadId, CancellationToken cancellationToken = default)
        {
            if (eadId <= 0)
            {
                throw new ArgumentException("Invalid UC id.", nameof(eadId));
            }

            var uc = await _educationRepository.GetUcByEadIdAsync(eadId, cancellationToken);
            if (uc == null || string.IsNullOrWhiteSpace(uc.Id))
            {
                return Array.Empty<Student>();
            }

            return await _educationRepository.GetStudentsByUcAsync(uc.Id, cancellationToken);
        }

        public async Task<IReadOnlyCollection<StudentUcDto>> GetStudentsByUcEadIdWithPerformanceAsync(int eadId, CancellationToken cancellationToken = default)
        {
            if (eadId <= 0)
            {
                throw new ArgumentException("Invalid UC id.", nameof(eadId));
            }

            var uc = await _educationRepository.GetUcByEadIdAsync(eadId, cancellationToken);
            if (uc == null || string.IsNullOrWhiteSpace(uc.Id))
            {
                return Array.Empty<StudentUcDto>();
            }

            return await _educationRepository.GetStudentsByUcWithPerformanceAsync(uc.Id, cancellationToken);
        }

        public async Task<IEnumerable<string>> GetUcsByStudentAsync(string studentId, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(studentId);
            return await _studentUcPerformanceRepository.GetUcsByStudentAsync(studentId);
        }

        public async Task<StudentUcPerformance?> GetStudentUcPerformanceAsync(string studentId, string ucId, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(studentId);
            ArgumentNullException.ThrowIfNull(ucId);
            return await _studentUcPerformanceRepository.GetByStudentAndUcAsync(studentId, ucId);
        }

        public async Task ToggleActivityHiddenAsync(string studentId, string ucId, string activityName, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(studentId);
            ArgumentNullException.ThrowIfNull(ucId);
            ArgumentNullException.ThrowIfNull(activityName);

            ThrowReadOnly();

            // Validar que a atividade existe
            var performance = await _studentUcPerformanceRepository.GetByStudentAndUcAsync(studentId, ucId);
            if (performance == null)
                throw new NotFoundException("Desempenho do estudante não encontrado");

            var activity = performance.Activities.FirstOrDefault(a => 
                a.Name.Equals(activityName, StringComparison.OrdinalIgnoreCase));
            if (activity == null)
                throw new NotFoundException("Atividade não encontrada");

            // Carregar ou criar config de atividades ocultas
            var config = await _studentUcPerformanceRepository.GetHiddenActivitiesAsync(ucId);
            if (config == null)
            {
                config = new HiddenActivitiesConfig { UcId = ucId };
            }

            // Toggle
            config.ToggleActivityName(activityName);
            await _studentUcPerformanceRepository.SaveHiddenActivitiesAsync(config);
        }

        private void ThrowReadOnly()
        {
            throw new BusinessException(_localizer["EducationReadOnly"]);
        }
    }
}
