using Application.Domain.Model.Todo;
using System.IO;

namespace Application.Domain.Interface.Todo
{
    public interface ITodoImageRepository
    {
        Task<string> StoreAsync(Stream stream, string contentType, string fileName, string? uploadedByUserId);
        Task<(byte[] Data, string ContentType, string FileName)?> GetAsync(string imageId);
    }
}
