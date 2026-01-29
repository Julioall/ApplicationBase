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

        // Métodos de hierarquia de categorias
        /// <summary>
        /// Retorna todas as instituições (depth 1) - Ex: SENAI, SESI
        /// </summary>
        Task<IReadOnlyCollection<MoodleCategory>> GetInstitutionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retorna todas as escolas (depth 2) de uma instituição
        /// </summary>
        Task<IReadOnlyCollection<MoodleCategory>> GetSchoolsByInstitutionAsync(int institutionMoodleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retorna todos os cursos (depth 3) de uma escola
        /// </summary>
        Task<IReadOnlyCollection<MoodleCategory>> GetCoursesBySchoolAsync(int schoolMoodleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retorna todos os eventos/turmas (depth 4) de um curso
        /// </summary>
        Task<IReadOnlyCollection<MoodleCategory>> GetEventsByCourseAsync(int courseMoodleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Constrói a hierarquia completa de categorias a partir do path
        /// </summary>
        Task<MoodleCategoryHierarchy> GetCategoryHierarchyAsync(string? path, CancellationToken cancellationToken = default);
    }
}
