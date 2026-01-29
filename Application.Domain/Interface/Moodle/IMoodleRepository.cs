using Application.Domain.Model.Dtos;
using Application.Domain.Model.Education.Dtos;
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
        Task<MoodleCourse?> GetCourseByMoodleIdAsync(int moodleId, CancellationToken cancellationToken = default);
        Task<MoodleCohortCourseMap> EnsureCohortCourseMapAsync(string cohortId, string courseId, CancellationToken cancellationToken = default);
        Task<StudentCourseMap> EnsureStudentCourseMapAsync(string studentId, string courseId, CancellationToken cancellationToken = default);
        Task EnsureStudentCourseMapBatchAsync(IEnumerable<StudentCourseMap> maps, CancellationToken cancellationToken = default);
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
        Task<MoodleCategory> UpsertCategoryByMoodleIdAsync(int moodleId, string name, int parentId, int depth, string? path, CancellationToken cancellationToken = default);
        Task<MoodleCategory?> GetCategoryByMoodleIdAsync(int moodleId, CancellationToken cancellationToken = default);
        Task<IReadOnlyDictionary<int, MoodleCategory>> GetCategoriesByMoodleIdsAsync(IEnumerable<int> moodleIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Batch upsert multiple categories efficiently in a single operation.
        /// </summary>
        Task<IReadOnlyDictionary<int, MoodleCategory>> UpsertCategoriesBatchAsync(IEnumerable<MoodleCategoryDto> categories, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retorna todas as categorias de um determinado nível de profundidade.
        /// Depth 1 = Instituição, Depth 2 = Escola, Depth 3 = Curso, Depth 4 = Evento/Turma
        /// </summary>
        Task<IReadOnlyCollection<MoodleCategory>> GetCategoriesByDepthAsync(int depth, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retorna todas as instituições (depth 1)
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
        /// Constrói a hierarquia completa de categorias a partir do path de uma categoria
        /// </summary>
        Task<MoodleCategoryHierarchy> GetCategoryHierarchyAsync(string? path, CancellationToken cancellationToken = default);
    }
}
