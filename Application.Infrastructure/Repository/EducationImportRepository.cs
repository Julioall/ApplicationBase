using Application.Domain.Interface;
using Application.Domain.Interface.Education;
using Application.Domain.Model.Education;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;

namespace Application.Infrastructure.Repository
{
    public class EducationImportRepository : IEducationImportRepository
    {
        private const string AttachmentName = "file";
        private readonly IServiceRavenDB _serviceRavenDb;

        public EducationImportRepository(IServiceRavenDB serviceRavenDb)
        {
            _serviceRavenDb = serviceRavenDb;
        }

        public async Task<EducationImport> AddAsync(EducationImport import, Stream fileStream, string contentType, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(import);
            ArgumentNullException.ThrowIfNull(fileStream);
            ArgumentException.ThrowIfNullOrWhiteSpace(import.FileName);

            await _serviceRavenDb.AsyncSession.StoreAsync(import, cancellationToken);
            var id = _serviceRavenDb.AsyncSession.Advanced.GetDocumentId(import);
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new InvalidOperationException("Import identifier could not be generated.");
            }

            import.Id = id;
            var buffer = new MemoryStream();
            await fileStream.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;
            _serviceRavenDb.AsyncSession.Advanced.Attachments.Store(import, AttachmentName, buffer, contentType);
            return import;
        }

        public Task<EducationImport?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            return _serviceRavenDb.AsyncSession.LoadAsync<EducationImport?>(id, cancellationToken);
        }

        public async Task<EducationImport?> GetNextPendingAsync(CancellationToken cancellationToken = default)
        {
            var import = await _serviceRavenDb.AsyncSession.Query<EducationImport>()
                .Where(x => x.Status == EducationImportStatus.Pending)
                .OrderBy(x => x.CreatedAt)
                .Take(1)
                .FirstOrDefaultAsync(cancellationToken);

            return import;
        }

        public async Task UpdateAsync(EducationImport import, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(import);
            await _serviceRavenDb.AsyncSession.StoreAsync(import, import.Id, cancellationToken);
        }

        public async Task<Stream?> GetImportFileAsync(string importId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(importId);
            var attachment = await _serviceRavenDb.AsyncSession.Advanced.Attachments.GetAsync(importId, AttachmentName, cancellationToken);
            return attachment?.Stream;
        }
    }
}
