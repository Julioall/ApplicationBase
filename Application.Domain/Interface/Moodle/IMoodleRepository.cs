using Application.Domain.Model.Dtos;
using Application.Domain.Model.Moodle;
using Application.Domain.Model.Moodle.Dtos;
using Application.Domain.Model.Students;

namespace Application.Domain.Interface.Moodle
{
    public interface IMoodleRepository
    {
        Task<MoodleCategory> UpsertCategoryAsync(string name, CancellationToken cancellationToken = default);
        Task<MoodleCourseCategory> UpsertCourseCategoryAsync(string categoryId, string name, CancellationToken cancellationToken = default);
        Task<MoodleCohort> UpsertCohortAsync(string categoryId, string courseCategoryId, string courseCategoryRaw, string name, CancellationToken cancellationToken = default);
        Task<CourseUpsertResult> UpsertCourseAsync(MoodleCourse course, CancellationToken cancellationToken = default);
        Task<MoodleCourse?> GetCourseByEadIdAsync(int eadId, CancellationToken cancellationToken = default);
        Task<MoodleCohortCourseMap> EnsureCohortCourseMapAsync(string cohortId, string courseId, CancellationToken cancellationToken = default);
        Task<StudentCourseMap> EnsureStudentCourseMapAsync(string studentId, string courseId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Student>> GetStudentsByCourseAsync(string courseId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<StudentCourseDto>> GetStudentsByCourseWithPerformanceAsync(string courseId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<MoodleCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<MoodleCourseCategory>> GetCourseCategoriesByCategoryAsync(string categoryId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<MoodleCohort>> GetCohortsByCourseCategoryAsync(string courseCategoryId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<MoodleCourse>> GetCoursesByCohortAsync(string cohortId, string? search = null, CancellationToken cancellationToken = default);
        Task<PagedResult<MoodleCourse>> SearchCoursesAsync(string? search, string? cohortId, string? courseCategoryId, PaginationQuery query, CancellationToken cancellationToken = default);
        Task RecalculateCohortPeriodAsync(string cohortId, CancellationToken cancellationToken = default);
        Task<MoodleSyncStatus> GetSyncStatusAsync(CancellationToken cancellationToken = default);
        Task UpdateSyncStatusAsync(MoodleSyncStatus status, CancellationToken cancellationToken = default);
    }
}
