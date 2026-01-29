using Application.Domain.Model.Dtos;
using Application.Domain.Model.Education;
using Application.Domain.Model.Education.Dtos;
using Application.Domain.Model.Students;

namespace Application.Service.Interface
{
    public interface IEducationService
    {
        Task<IReadOnlyCollection<School>> GetSchoolsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<ProgramDocument>> GetProgramsBySchoolAsync(string schoolId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<ClassDocument>> GetClassesByProgramAsync(string programId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<CourseUnit>> GetCourseUnitsByClassAsync(string classId, string? search = null, CancellationToken cancellationToken = default);
        Task<PagedResult<CourseUnit>> SearchCourseUnitsAsync(PaginationQuery query, string? classId = null, string? programId = null, CancellationToken cancellationToken = default);
        Task<EducationSyncStatus> GetSyncStatusAsync(CancellationToken cancellationToken = default);
        Task<EducationSyncStatus> TriggerSyncAsync(string userId, string? userName, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Student>> GetStudentsByCourseUnitEadIdAsync(int eadId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<StudentUcDto>> GetStudentsByCourseUnitEadIdWithPerformanceAsync(int eadId, CancellationToken cancellationToken = default);
        Task<IEnumerable<string>> GetCourseUnitsByStudentAsync(string studentId, CancellationToken cancellationToken = default);
        Task<StudentCourseUnitPerformance?> GetStudentCourseUnitPerformanceAsync(string studentId, string courseUnitId, CancellationToken cancellationToken = default);
        Task ToggleActivityHiddenAsync(string studentId, string courseUnitId, string activityName, CancellationToken cancellationToken = default);
    }
}
