using Application.Domain.Model.Dtos;
using Application.Domain.Model.Education;
using Application.Domain.Model.Education.Dtos;
using Application.Domain.Model.Students;

namespace Application.Service.Interface
{
    public interface IEducationService
    {
        Task<CourseImportResult> ImportCoursesAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
        Task<EducationImport> EnqueueImportAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
        Task<EducationImport?> GetImportAsync(string id, CancellationToken cancellationToken = default);
        Task<EducationReportImport> EnqueueReportImportAsync(IEnumerable<(string fileName, Stream fileStream)> files, CancellationToken cancellationToken = default);
        Task<EducationReportImportResult> ImportReportAsync(IEnumerable<(string fileName, Stream fileStream)> files, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<School>> GetSchoolsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<ProgramDocument>> GetProgramsBySchoolAsync(string schoolId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<ClassDocument>> GetClassesByProgramAsync(string programId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<UcDocument>> GetUcsByClassAsync(string classId, string? search = null, CancellationToken cancellationToken = default);
        Task<PagedResult<UcDocument>> SearchUcsAsync(PaginationQuery query, string? classId = null, string? programId = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Student>> GetStudentsByUcEadIdAsync(int eadId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<StudentUcDto>> GetStudentsByUcEadIdWithPerformanceAsync(int eadId, CancellationToken cancellationToken = default);
        Task<IEnumerable<string>> GetUcsByStudentAsync(string studentId, CancellationToken cancellationToken = default);
        Task<StudentUcPerformance?> GetStudentUcPerformanceAsync(string studentId, string ucId, CancellationToken cancellationToken = default);
        Task ToggleActivityHiddenAsync(string studentId, string ucId, string activityName, CancellationToken cancellationToken = default);
    }
}
