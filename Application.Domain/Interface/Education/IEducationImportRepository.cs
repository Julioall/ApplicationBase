using Application.Domain.Model.Education;

namespace Application.Domain.Interface.Education
{
    public interface IEducationImportRepository
    {
        Task<EducationImport> AddAsync(EducationImport import, Stream fileStream, string contentType, CancellationToken cancellationToken = default);
        Task<EducationImport?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<EducationImport?> GetNextPendingAsync(CancellationToken cancellationToken = default);
        Task UpdateAsync(EducationImport import, CancellationToken cancellationToken = default);
        Task<Stream?> GetImportFileAsync(string importId, CancellationToken cancellationToken = default);
    }
}
