using Application.Domain.Model.User;
using System.IO;

namespace Application.Service.Interface
{
    public interface IUserService
    {
        Task AddAsync(User user, string password);

        Task DeleteAsync(string id);

        Task UpdateAsync(User user);

        Task<IEnumerable<User>> GetAllAsync();

        Task<User> GetByIdAsync(string id);

        Task<User> GetByEmailAsync(string email);

        Task<IEnumerable<User>> GetByPermissionAsync(string permission);

        Task<User> GetByRefreshTokenAsync(string refreshTokenId);

        Task UpdateProfileAsync(string email, string? name, DateTime? dateOfBirth, Stream? profilePictureStream, string? profilePictureContentType, bool removeProfilePicture, double? profilePictureOffsetX, double? profilePictureOffsetY, string? jobTitle, string? department, string? organization, string? location, double? profilePictureScale);

        Task ChangePasswordAsync(string email, string currentPassword, string newPassword);

        Task<(byte[] Data, string ContentType)?> GetProfilePictureAsync(string userId);

        Task<(string Code, DateTime ExpiresAt)> GenerateRecoveryCodeAsync(string email, bool sendEmail);

        Task ValidateRecoveryCodeAsync(string email, string code);

        Task ChangePasswordWithRecoveryCodeAsync(string email, string code, string newPassword);
    }
}
