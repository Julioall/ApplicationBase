using Application.Domain.Model.Dtos;
using Application.Domain.Model.Education;
using Application.Domain.Model.Education.Dtos;

namespace Application.Service.Interface
{
    public interface IEducationService
    {
        Task<EducationImport> EnqueueImportAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
        Task<EducationImport?> GetImportAsync(string id, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<School>> GetSchoolsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<ProgramDocument>> GetProgramsBySchoolAsync(string schoolId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<ClassDocument>> GetClassesByProgramAsync(string programId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<UcDocument>> GetUcsByClassAsync(string classId, string? search = null, CancellationToken cancellationToken = default);
        Task<PagedResult<UcDocument>> SearchUcsAsync(PaginationQuery query, string? classId = null, string? programId = null, CancellationToken cancellationToken = default);
    }
}
