using Application.Domain.Model.User;
using System.IO;

namespace Application.Service.Interface
{
    public interface IUserService
    {
        Task AddAsync(User user);

        Task DeleteAsync(string id);

        Task UpdateAsync(User user);

        Task<IEnumerable<User>> GetAllAsync();

        Task<User> GetByIdAsync(string id);

        Task<User> GetByEmailAsync(string email);

        Task<User> GetByRoleAsync(string role);

        Task<User> GetByRefreshTokenAsync(string refreshToken);

        Task UpdateProfileAsync(string email, string? name, DateTime? dateOfBirth, Stream? profilePictureStream, string? profilePictureContentType, bool removeProfilePicture, double? profilePictureOffsetX, double? profilePictureOffsetY, string? jobTitle, string? department, string? organization, string? location, double? profilePictureScale);

        Task ChangePasswordAsync(string email, string currentPassword, string newPassword);

        Task<(byte[] Data, string ContentType)?> GetProfilePictureAsync(string userId);
    }
}
