using Application.Domain.Interface.Todo;
using Application.Domain.Model.Todo;
using Application.Infrastructure.Service;
using System.IO;
using System.Linq;

namespace Application.Infrastructure.Repository.Todo
{
    public class TodoImageRepository : ITodoImageRepository
    {
        private const string AttachmentName = "file";
        private readonly IServiceRavenDB _serviceRavenDb;

        public TodoImageRepository(IServiceRavenDB serviceRavenDb)
        {
            _serviceRavenDb = serviceRavenDb;
        }

        public async Task<string> StoreAsync(Stream stream, string contentType, string fileName, string? uploadedByUserId)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

            var imageId = $"TodoImage-{Guid.NewGuid():N}";
            var image = new TodoImage
            {
                Id = imageId,
                FileName = SanitizeFileName(fileName),
                ContentType = contentType,
                Length = stream.Length,
                UploadedByUserId = uploadedByUserId,
                CreatedAt = DateTime.UtcNow
            };

            await _serviceRavenDb.AsyncSession.StoreAsync(image);

            stream.Position = 0;
            _serviceRavenDb.AsyncSession.Advanced.Attachments.Store(imageId, AttachmentName, stream, contentType);

            await _serviceRavenDb.AsyncSession.SaveChangesAsync();

            return imageId;
        }

        public async Task<(byte[] Data, string ContentType, string FileName)?> GetAsync(string imageId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(imageId);

            using var attachment = await _serviceRavenDb.AsyncSession.Advanced.Attachments.GetAsync(imageId, AttachmentName);
            if (attachment == null)
            {
                return null;
            }

            using var memory = new MemoryStream();
            await attachment.Stream.CopyToAsync(memory);
            return (memory.ToArray(), attachment.Details.ContentType, attachment.Details.Name);
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var cleaned = new string(fileName.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                return "image";
            }

            return cleaned.Length > 140 ? cleaned[..140] : cleaned;
        }
    }
}
