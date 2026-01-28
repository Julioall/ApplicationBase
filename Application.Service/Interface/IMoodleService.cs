using Application.Domain.Model.Dtos;
using Application.Domain.Model.Moodle;
using Application.Domain.Model.Moodle.Dtos;
using Application.Domain.Model.Students;

namespace Application.Service.Interface
{
    public interface IMoodleService
    {
        Task<IReadOnlyCollection<MoodleCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<MoodleCourseCategory>> GetCourseCategoriesByCategoryAsync(string categoryId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<MoodleCohort>> GetCohortsByCourseCategoryAsync(string courseCategoryId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<MoodleCourse>> GetCoursesByCohortAsync(string cohortId, string? search = null, CancellationToken cancellationToken = default);
        Task<PagedResult<MoodleCourse>> SearchCoursesAsync(PaginationQuery query, string? cohortId = null, string? courseCategoryId = null, CancellationToken cancellationToken = default);
        Task<MoodleSyncStatus> GetSyncStatusAsync(CancellationToken cancellationToken = default);
        Task<MoodleSyncStatus> TriggerSyncAsync(string userId, string? userName, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Student>> GetStudentsByCourseEadIdAsync(int eadId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<StudentCourseDto>> GetStudentsByCourseEadIdWithPerformanceAsync(int eadId, CancellationToken cancellationToken = default);
        Task<IEnumerable<string>> GetCoursesByStudentAsync(string studentId, CancellationToken cancellationToken = default);
        Task<StudentCoursePerformance?> GetStudentCoursePerformanceAsync(string studentId, string courseId, CancellationToken cancellationToken = default);
        Task ToggleActivityHiddenAsync(string studentId, string courseId, string activityName, CancellationToken cancellationToken = default);
    }
}
