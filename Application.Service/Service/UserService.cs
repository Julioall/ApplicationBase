using Application.Domain;
using Application.Domain.Exceptions;
using Application.Domain.Interface;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.User;
using Application.Service.Interface;
using Application.Service.Service.Security;
using FluentValidation;
using Microsoft.Extensions.Localization;
using System.IO;

namespace Application.Service.Service
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IValidator<User> _userValidator;
        private readonly IValidator<PasswordInput> _passwordValidator;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public UserService(IUserRepository userRepository, IValidator<User> userValidator, IValidator<PasswordInput> passwordValidator, IStringLocalizer<SharedResource> localizer)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _userValidator = userValidator ?? throw new ArgumentNullException(nameof(userValidator));
            _passwordValidator = passwordValidator ?? throw new ArgumentNullException(nameof(passwordValidator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task AddAsync(User user, string password)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentException.ThrowIfNullOrWhiteSpace(password);

            _passwordValidator.ValidateAndThrow(new PasswordInput { Password = password });
            user.Account.PasswordHash = SecureHash.HashSecret(password);
            user.Account.DateJoined ??= DateTime.UtcNow;

            _userValidator.ValidateAndThrow(user);

            var existingUser = await _userRepository.GetByEmailAsync(user.Account.Email);
            if (existingUser != null)
            {
                throw new ConflictException(_localizer["EmailAlreadyExists"]);
            }

            await _userRepository.AddAsync(user);
        }

        public Task DeleteAsync(string id)
        {
            return _userRepository.DeleteAsync(id);
        }

        public async Task UpdateAsync(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            _userValidator.ValidateAndThrow(user);
            await _userRepository.UpdateAsync(user);
        }

        public Task<IEnumerable<User>> GetAllAsync()
        {
            return _userRepository.GetAllAsync();
        }

        public Task<User> GetByIdAsync(string id)
        {
            return _userRepository.GetByIdAsync(id);
        }

        public Task<User> GetByEmailAsync(string email)
        {
            return _userRepository.GetByEmailAsync(email);
        }

        public Task<User> GetByRoleAsync(string role)
        {
            return _userRepository.GetByRoleAsync(role);
        }

        public Task<User> GetByRefreshTokenAsync(string refreshTokenId)
        {
            return _userRepository.GetByRefreshTokenAsync(refreshTokenId);
        }

        public async Task UpdateProfileAsync(string email, string? name, DateTime? dateOfBirth, Stream? profilePictureStream, string? profilePictureContentType, bool removeProfilePicture, double? profilePictureOffsetX, double? profilePictureOffsetY, string? jobTitle, string? department, string? organization, string? location, double? profilePictureScale)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(email);

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                throw new NotFoundException(_localizer["UserNotFoundByEmail", email]);
            }

            user.Profile ??= new UserProfile { Name = string.Empty };

            var trimmedName = name?.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedName))
            {
                user.Profile.Name = trimmedName;
            }

            user.Profile.DateOfBirth = dateOfBirth;
            if (profilePictureOffsetX.HasValue)
            {
                user.Profile.ProfilePictureOffsetX = profilePictureOffsetX.Value;
            }

            if (profilePictureOffsetY.HasValue)
            {
                user.Profile.ProfilePictureOffsetY = profilePictureOffsetY.Value;
            }

            user.Profile.JobTitle = Normalize(jobTitle);
            user.Profile.Department = Normalize(department);
            user.Profile.Organization = Normalize(organization);
            user.Profile.Location = Normalize(location);
            if (profilePictureScale.HasValue)
            {
                user.Profile.ProfilePictureScale = profilePictureScale.Value;
            }

            await HandleProfilePictureAsync(user, profilePictureStream, profilePictureContentType, removeProfilePicture);

            _userValidator.ValidateAndThrow(user);
            await _userRepository.UpdateAsync(user);
        }

        public async Task ChangePasswordAsync(string email, string currentPassword, string newPassword)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(email);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentPassword);
            ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                throw new NotFoundException(_localizer["UserNotFoundByEmail", email]);
            }

            if (!SecureHash.Verify(currentPassword, user.Account.PasswordHash))
            {
                throw new BusinessException(_localizer["InvalidCurrentPassword"]);
            }

            _passwordValidator.ValidateAndThrow(new PasswordInput { Password = newPassword });
            user.Account.PasswordHash = SecureHash.HashSecret(newPassword);
            user.Account.RefreshTokenHash = null;
            user.Account.RefreshTokenId = null;
            user.Account.RefreshTokenExpiry = null;

            _userValidator.ValidateAndThrow(user);
            await _userRepository.UpdateAsync(user);
        }

        public Task<(byte[] Data, string ContentType)?> GetProfilePictureAsync(string userId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);
            return _userRepository.GetProfilePictureAsync(userId);
        }

        private async Task HandleProfilePictureAsync(User user, Stream? profilePictureStream, string? profilePictureContentType, bool removeProfilePicture)
        {
            if (string.IsNullOrEmpty(user.Id))
            {
                return;
            }

            if (removeProfilePicture)
            {
                await _userRepository.DeleteProfilePictureAsync(user.Id);
                user.Profile!.ProfilePictureUrl = null;
                return;
            }

            if (profilePictureStream == null)
            {
                return;
            }

            if (profilePictureStream.CanSeek)
            {
                profilePictureStream.Seek(0, SeekOrigin.Begin);
            }

            var contentType = string.IsNullOrWhiteSpace(profilePictureContentType) ? "application/octet-stream" : profilePictureContentType;
            await _userRepository.UploadProfilePictureAsync(user.Id, profilePictureStream, contentType);
            user.Profile!.ProfilePictureUrl = null;
        }

        private static string? Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            return trimmed.Length == 0 ? null : trimmed;
        }
    }
}
