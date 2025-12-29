using Application.Domain.Model.User;
using System.IO;

namespace Application.Domain.Interface
{
    public interface IUserRepository
    {
        Task AddAsync(User user);
        Task DeleteAsync(string id);
        Task<IEnumerable<User>> GetAllAsync();
        Task<bool> AnyAsync();
        Task<User> GetByIdAsync(string id);
        Task<IEnumerable<User>> GetByPermissionAsync(string permission);
        Task<User> GetByEmailAsync(string email);
        Task<User> GetByRefreshTokenAsync(string refreshTokenId);
        Task UpdateAsync(User user);
        Task UploadProfilePictureAsync(string userId, Stream stream, string contentType);
        Task<(byte[] Data, string ContentType)?> GetProfilePictureAsync(string userId);
        Task DeleteProfilePictureAsync(string userId);
    }
}
