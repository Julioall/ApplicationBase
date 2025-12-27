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
        Task<UcUpsertResult> UpsertUcAsync(UcDocument uc, CancellationToken cancellationToken = default);
        Task<UcDocument?> GetUcByEadIdAsync(int eadId, CancellationToken cancellationToken = default);
        Task<ClassUcMap> EnsureClassUcMapAsync(string classId, string ucId, CancellationToken cancellationToken = default);
        Task<StudentUcMap> EnsureStudentUcMapAsync(string studentId, string ucId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Student>> GetStudentsByUcAsync(string ucId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<School>> GetSchoolsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<ProgramDocument>> GetProgramsBySchoolAsync(string schoolId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<ClassDocument>> GetClassesByProgramAsync(string programId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<UcDocument>> GetUcsByClassAsync(string classId, string? search = null, CancellationToken cancellationToken = default);
        Task<PagedResult<UcDocument>> SearchUcsAsync(string? search, string? classId, string? programId, PaginationQuery query, CancellationToken cancellationToken = default);
        Task RecalculateClassPeriodAsync(string classId, CancellationToken cancellationToken = default);
    }
}
