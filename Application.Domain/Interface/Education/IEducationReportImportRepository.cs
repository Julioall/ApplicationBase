using Application.Domain.Model.Education;

namespace Application.Domain.Interface.Education
{
    public interface IEducationReportImportRepository
    {
        Task<EducationReportImport> AddAsync(
            EducationReportImport import,
            IReadOnlyCollection<(string FileName, Stream FileStream)> files,
            string contentType,
            CancellationToken cancellationToken = default);

        Task<EducationReportImport?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

        Task UpdateAsync(EducationReportImport import, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<(string FileName, Stream FileStream)>> GetImportFilesAsync(
            string importId,
            CancellationToken cancellationToken = default);
    }
}
