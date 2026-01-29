using Application.Domain.Model.Dtos;
using Application.Domain.Model.Education;
using Application.Domain.Model.Education.Dtos;
using Application.Domain.Model.Students;

namespace Application.Domain.Interface.Education
{
    public interface IEducationRepository
    {
        Task<School> UpsertSchoolAsync(string name, CancellationToken cancellationToken = default);
        Task<ProgramDocument> UpsertProgramAsync(string schoolId, string name, CancellationToken cancellationToken = default);
        Task<ClassDocument> UpsertClassAsync(string schoolId, string programId, string courseCategoryRaw, string name, CancellationToken cancellationToken = default);
        Task<CourseUnitUpsertResult> UpsertCourseUnitAsync(CourseUnit courseUnit, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<CourseUnitUpsertResult>> UpsertCourseUnitBatchAsync(IEnumerable<CourseUnit> courseUnits, CancellationToken cancellationToken = default);
        Task<CourseUnit?> GetCourseUnitByEadIdAsync(int eadId, CancellationToken cancellationToken = default);
        Task<ClassUcMap> EnsureClassCourseUnitMapAsync(string classId, string courseUnitId, CancellationToken cancellationToken = default);
        Task<StudentUcMap> EnsureStudentCourseUnitMapAsync(string studentId, string courseUnitId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Student>> GetStudentsByCourseUnitAsync(string courseUnitId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<StudentUcDto>> GetStudentsByCourseUnitWithPerformanceAsync(string courseUnitId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<School>> GetSchoolsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<ProgramDocument>> GetProgramsBySchoolAsync(string schoolId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<ClassDocument>> GetClassesByProgramAsync(string programId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<CourseUnit>> GetCourseUnitsByClassAsync(string classId, string? search = null, CancellationToken cancellationToken = default);
        Task<PagedResult<CourseUnit>> SearchCourseUnitsAsync(string? search, string? classId, string? programId, PaginationQuery query, CancellationToken cancellationToken = default);
        Task RecalculateClassPeriodAsync(string classId, CancellationToken cancellationToken = default);
        Task<EducationSyncStatus> GetSyncStatusAsync(CancellationToken cancellationToken = default);
        Task UpdateSyncStatusAsync(EducationSyncStatus status, CancellationToken cancellationToken = default);
    }
}
