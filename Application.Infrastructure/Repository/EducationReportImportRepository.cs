using System.Linq;
using Application.Domain.Interface.Education;
using Application.Domain.Model.Education;
using Application.Infrastructure.Service;

namespace Application.Infrastructure.Repository
{
    public class EducationReportImportRepository : IEducationReportImportRepository
    {
        private const string DefaultContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private readonly IServiceRavenDB _serviceRavenDb;

        public EducationReportImportRepository(IServiceRavenDB serviceRavenDb)
        {
            _serviceRavenDb = serviceRavenDb;
        }

        public async Task<EducationReportImport> AddAsync(
            EducationReportImport import,
            IReadOnlyCollection<(string FileName, Stream FileStream)> files,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(import);
            ArgumentNullException.ThrowIfNull(files);

            await _serviceRavenDb.AsyncSession.StoreAsync(import, cancellationToken);
            var id = _serviceRavenDb.AsyncSession.Advanced.GetDocumentId(import);
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new InvalidOperationException("Import identifier could not be generated.");
            }

            import.Id = id;
            var index = 0;
            foreach (var (fileName, fileStream) in files)
            {
                ArgumentNullException.ThrowIfNull(fileStream);
                var buffer = new MemoryStream();
                await fileStream.CopyToAsync(buffer, cancellationToken);
                buffer.Position = 0;
                fileStream.Dispose();

                var attachmentName = $"file-{index}";
                var content = string.IsNullOrWhiteSpace(contentType) ? DefaultContentType : contentType;
                _serviceRavenDb.AsyncSession.Advanced.Attachments.Store(import, attachmentName, buffer, content);

                import.FileNames.Add(fileName);
                import.AttachmentNames.Add(attachmentName);
                index++;
            }

            // Persist the import and attachments so background jobs can read them immediately
            await _serviceRavenDb.AsyncSession.SaveChangesAsync(cancellationToken);

            return import;
        }

        public Task<EducationReportImport?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            return _serviceRavenDb.AsyncSession.LoadAsync<EducationReportImport?>(id, cancellationToken);
        }

        public async Task UpdateAsync(EducationReportImport import, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(import);
            await _serviceRavenDb.AsyncSession.StoreAsync(import, import.Id, cancellationToken);
        }

        public async Task<IReadOnlyCollection<(string FileName, Stream FileStream)>> GetImportFilesAsync(
            string importId,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(importId);

            var import = await _serviceRavenDb.AsyncSession.LoadAsync<EducationReportImport?>(importId, cancellationToken);
            if (import == null || import.AttachmentNames.Count == 0)
            {
                return Array.Empty<(string FileName, Stream FileStream)>();
            }

            var result = new List<(string FileName, Stream FileStream)>(import.AttachmentNames.Count);
            for (var i = 0; i < import.AttachmentNames.Count; i++)
            {
                var attachmentName = import.AttachmentNames[i];
                var fileName = import.FileNames.ElementAtOrDefault(i) ?? attachmentName;
                var attachment = await _serviceRavenDb.AsyncSession.Advanced.Attachments.GetAsync(importId, attachmentName, cancellationToken);
                if (attachment?.Stream == null)
                {
                    continue;
                }

                var copy = new MemoryStream();
                await attachment.Stream.CopyToAsync(copy, cancellationToken);
                copy.Position = 0;
                attachment.Stream.Dispose();
                result.Add((fileName, copy));
            }

            return result;
        }
    }
}
