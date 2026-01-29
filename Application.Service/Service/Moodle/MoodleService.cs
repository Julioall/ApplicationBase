using Application.Domain.Exceptions;
using Application.Domain.Interface.Moodle;
using Application.Domain.Localization;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.Moodle;
using Application.Domain.Model.Moodle.Dtos;
using Application.Domain.Model.Students;
using Application.Service.Interface;
using Application.Shared.Background;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Application.Service.Service.Moodle
{
    public class MoodleService : IMoodleService
    {
        private readonly IMoodleRepository _moodleRepository;
        private readonly IStudentCoursePerformanceRepository _studentCoursePerformanceRepository;
        private readonly IBackgroundJobScheduler _backgroundJobScheduler;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ILogger<MoodleService> _logger;
        private const string SyncStatusRunning = "Running";
        private const int ManualSyncCooldownSeconds = 300;

        public MoodleService(
            IMoodleRepository moodleRepository,
            IStudentCoursePerformanceRepository studentCoursePerformanceRepository,
            IBackgroundJobScheduler backgroundJobScheduler,
            IStringLocalizer<SharedResource> localizer,
            ILogger<MoodleService> logger)
        {
            _moodleRepository = moodleRepository;
            _studentCoursePerformanceRepository = studentCoursePerformanceRepository;
            _backgroundJobScheduler = backgroundJobScheduler;
            _localizer = localizer;
            _logger = logger;
        }

        public Task<IReadOnlyCollection<MoodleCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        {
            return _moodleRepository.GetCategoriesAsync(cancellationToken);
        }

        public Task<IReadOnlyCollection<MoodleCourseCategory>> GetCourseCategoriesByCategoryAsync(string categoryId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(categoryId);
            return _moodleRepository.GetCourseCategoriesByCategoryAsync(categoryId, cancellationToken);
        }

        public Task<IReadOnlyCollection<MoodleCohort>> GetCohortsByCourseCategoryAsync(string courseCategoryId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(courseCategoryId);
            return _moodleRepository.GetCohortsByCourseCategoryAsync(courseCategoryId, cancellationToken);
        }

        public Task<IReadOnlyCollection<MoodleCourse>> GetCoursesByCohortAsync(string cohortId, string? search = null, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cohortId);
            return _moodleRepository.GetCoursesByCohortAsync(cohortId, search, cancellationToken);
        }

        public Task<PagedResult<MoodleCourse>> SearchCoursesAsync(PaginationQuery query, string? cohortId = null, string? courseCategoryId = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);
            return _moodleRepository.SearchCoursesAsync(query.Search, cohortId, courseCategoryId, query, cancellationToken);
        }

        public Task<MoodleSyncStatus> GetSyncStatusAsync(CancellationToken cancellationToken = default)
        {
            return _moodleRepository.GetSyncStatusAsync(cancellationToken);
        }

        public async Task<MoodleSyncStatus> TriggerSyncAsync(string userId, string? userName, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);

            var status = await _moodleRepository.GetSyncStatusAsync(cancellationToken);
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (string.Equals(status.Status, SyncStatusRunning, StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException(_localizer["MoodleSyncAlreadyRunning"]);
            }

            if (status.TriggeredAt.HasValue && now - status.TriggeredAt.Value < ManualSyncCooldownSeconds)
            {
                var minutes = (int)Math.Ceiling(ManualSyncCooldownSeconds / 60.0);
                throw new TooManyRequestsException(_localizer["MoodleSyncRateLimited", minutes]);
            }

            status.Status = SyncStatusRunning;
            status.Message = _localizer["MoodleSyncQueued"];
            status.TriggeredAt = now;
            status.TriggeredByUserId = userId;
            status.TriggeredByName = string.IsNullOrWhiteSpace(userName) ? null : userName;

            await _moodleRepository.UpdateSyncStatusAsync(status, cancellationToken);

            // Enqueue the background job
            var triggeredByUserId = userId;
            var triggeredByName = userName ?? string.Empty;
            _backgroundJobScheduler.Enqueue<IMoodleSyncJob>(job => job.RunAsync(triggeredByUserId, triggeredByName, CancellationToken.None));

            _logger.LogInformation("Moodle manual sync triggered and queued by {UserId}", userId);

            return status;
        }

        public async Task<IReadOnlyCollection<Student>> GetStudentsByCourseEadIdAsync(int eadId, CancellationToken cancellationToken = default)
        {
            if (eadId <= 0)
            {
                throw new ArgumentException("Invalid course id.", nameof(eadId));
            }

            var course = await _moodleRepository.GetCourseByMoodleIdAsync(eadId, cancellationToken);
            if (course == null || string.IsNullOrWhiteSpace(course.Id))
            {
                return Array.Empty<Student>();
            }

            return await _moodleRepository.GetStudentsByCourseAsync(course.Id, cancellationToken);
        }

        public async Task<IReadOnlyCollection<StudentCourseDto>> GetStudentsByCourseEadIdWithPerformanceAsync(int eadId, CancellationToken cancellationToken = default)
        {
            if (eadId <= 0)
            {
                throw new ArgumentException("Invalid course id.", nameof(eadId));
            }

            var course = await _moodleRepository.GetCourseByMoodleIdAsync(eadId, cancellationToken);
            if (course == null || string.IsNullOrWhiteSpace(course.Id))
            {
                return Array.Empty<StudentCourseDto>();
            }

            return await _moodleRepository.GetStudentsByCourseWithPerformanceAsync(course.Id, cancellationToken);
        }

        public async Task<IEnumerable<string>> GetCoursesByStudentAsync(string studentId, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(studentId);
            return await _studentCoursePerformanceRepository.GetCoursesByStudentAsync(studentId);
        }

        public async Task<StudentCoursePerformance?> GetStudentCoursePerformanceAsync(string studentId, string courseId, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(studentId);
            ArgumentNullException.ThrowIfNull(courseId);
            return await _studentCoursePerformanceRepository.GetByStudentAndCourseAsync(studentId, courseId);
        }

        public async Task ToggleActivityHiddenAsync(string studentId, string courseId, string activityName, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(studentId);
            ArgumentNullException.ThrowIfNull(courseId);
            ArgumentNullException.ThrowIfNull(activityName);

            ThrowReadOnly();

            var performance = await _studentCoursePerformanceRepository.GetByStudentAndCourseAsync(studentId, courseId);
            if (performance == null)
                throw new NotFoundException("Desempenho do estudante não encontrado");

            var activity = performance.Activities.FirstOrDefault(a =>
                a.Name.Equals(activityName, StringComparison.OrdinalIgnoreCase));
            if (activity == null)
                throw new NotFoundException("Atividade não encontrada");

            var config = await _studentCoursePerformanceRepository.GetHiddenActivitiesAsync(courseId);
            if (config == null)
            {
                config = new HiddenActivitiesConfig { CourseId = courseId };
            }

            config.ToggleActivityName(activityName);
            await _studentCoursePerformanceRepository.SaveHiddenActivitiesAsync(config);
        }

        #region Category Hierarchy Methods

        public Task<IReadOnlyCollection<MoodleCategory>> GetInstitutionsAsync(CancellationToken cancellationToken = default)
        {
            return _moodleRepository.GetInstitutionsAsync(cancellationToken);
        }

        public Task<IReadOnlyCollection<MoodleCategory>> GetSchoolsByInstitutionAsync(int institutionMoodleId, CancellationToken cancellationToken = default)
        {
            if (institutionMoodleId <= 0)
            {
                throw new ArgumentException("Invalid institution Moodle ID.", nameof(institutionMoodleId));
            }

            return _moodleRepository.GetSchoolsByInstitutionAsync(institutionMoodleId, cancellationToken);
        }

        public Task<IReadOnlyCollection<MoodleCategory>> GetCoursesBySchoolAsync(int schoolMoodleId, CancellationToken cancellationToken = default)
        {
            if (schoolMoodleId <= 0)
            {
                throw new ArgumentException("Invalid school Moodle ID.", nameof(schoolMoodleId));
            }

            return _moodleRepository.GetCoursesBySchoolAsync(schoolMoodleId, cancellationToken);
        }

        public Task<IReadOnlyCollection<MoodleCategory>> GetEventsByCourseAsync(int courseMoodleId, CancellationToken cancellationToken = default)
        {
            if (courseMoodleId <= 0)
            {
                throw new ArgumentException("Invalid course Moodle ID.", nameof(courseMoodleId));
            }

            return _moodleRepository.GetEventsByCourseAsync(courseMoodleId, cancellationToken);
        }

        public Task<MoodleCategoryHierarchy> GetCategoryHierarchyAsync(string? path, CancellationToken cancellationToken = default)
        {
            return _moodleRepository.GetCategoryHierarchyAsync(path, cancellationToken);
        }

        #endregion

        private void ThrowReadOnly()
        {
            throw new BusinessException(_localizer["MoodleReadOnly"]);
        }
    }
}
