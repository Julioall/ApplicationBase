using Application.Domain.Model.User;
using System.IO;

namespace Application.Domain.Interface
{
    public interface IUserRepository
    {
        Task AddAsync(User user);
        Task DeleteAsync(string id);
        Task<IEnumerable<User>> GetAllAsync();
        Task<User> GetByIdAsync(string id);
        Task<User> GetByRoleAsync(string role);
        Task<User> GetByEmailAsync(string username);
        Task<User> GetByRefreshTokenAsync(string refreshTokenId);
        Task UpdateAsync(User user);
        Task UploadProfilePictureAsync(string userId, Stream stream, string contentType);
        Task<(byte[] Data, string ContentType)?> GetProfilePictureAsync(string userId);
        Task DeleteProfilePictureAsync(string userId);
    }
}
