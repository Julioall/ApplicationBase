using Application.Domain.Exceptions;
using Application.Domain.Interface;
using Application.Domain.Localization;
using Application.Domain.Model;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.User;
using Application.Service.Interface;
using Application.Service.Service.Security;
using FluentValidation;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Application.Service.Service
{
    public class UserService : IUserService
    {
        /// <summary>
        /// Adiciona ou atualiza um usuário externo (ex: Moodle) sem exigir senha.
        /// </summary>
        public async Task AddOrUpdateExternalUserAsync(User user)
        {
            ArgumentNullException.ThrowIfNull(user);
            _userValidator.ValidateAndThrow(user);

            var existingUser = await _userRepository.GetByEmailAsync(user.Account.Email);
            if (existingUser != null)
            {
                // Atualiza apenas dados externos
                existingUser.Account.Username = user.Account.Username;
                existingUser.Account.MoodleId = user.Account.MoodleId;
                existingUser.Profile.Name = user.Profile.Name;
                await _userRepository.UpdateAsync(existingUser);
            }
            else
            {
                await _userRepository.AddAsync(user);
            }
        }
        private readonly IUserRepository _userRepository;
        private readonly IValidator<User> _userValidator;
        private readonly IValidator<PasswordInput> _passwordValidator;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly IEmailService _emailService;
        private const int RecoveryCodeLength = 6;
        private const int RecoveryCodeTtlMinutes = 10;
        private const int MaxRecoveryAttempts = 5;
        private static readonly TimeSpan RecoveryCodeCooldown = TimeSpan.FromMinutes(1);

        public UserService(IUserRepository userRepository, IValidator<User> userValidator, IValidator<PasswordInput> passwordValidator, IStringLocalizer<SharedResource> localizer, IEmailService emailService)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _userValidator = userValidator ?? throw new ArgumentNullException(nameof(userValidator));
            _passwordValidator = passwordValidator ?? throw new ArgumentNullException(nameof(passwordValidator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        public async Task AddAsync(User user, string password)
        {
            ArgumentNullException.ThrowIfNull(user);
            ArgumentException.ThrowIfNullOrWhiteSpace(password);

            _passwordValidator.ValidateAndThrow(new PasswordInput { Password = password });
            user.Account.PasswordHash = SecureHash.HashSecret(password);
            user.Account.DateJoined ??= DateTime.UtcNow;

            var isFirstUser = !await _userRepository.AnyAsync();
            if (isFirstUser)
            {
                user.Account.Permissions = ApplicationPermissions.All.ToList();
            }

            EnsurePermissions(user);

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

            EnsurePermissions(user);
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

        public Task<IEnumerable<User>> GetByPermissionAsync(string permission)
        {
            return _userRepository.GetByPermissionAsync(permission);
        }

        public Task<User> GetByRefreshTokenAsync(string refreshTokenId)
        {
            return _userRepository.GetByRefreshTokenAsync(refreshTokenId);
        }

        public async Task UpdateProfileAsync(string email, string? name, Stream? profilePictureStream, string? profilePictureContentType, bool removeProfilePicture, double? profilePictureOffsetX, double? profilePictureOffsetY, string? jobTitle, string? department, string? organization, string? location, double? profilePictureScale)
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

            EnsurePermissions(user);
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

        public async Task<(string Code, DateTime ExpiresAt)> GenerateRecoveryCodeAsync(string email, bool sendEmail)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(email);

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                // Não vazar existência: retorna placeholders, controller pode responder genericamente
                return (string.Empty, DateTime.UtcNow);
            }

            if (user.Account.RecoveryCodeLastGenerated.HasValue &&
                DateTime.UtcNow - user.Account.RecoveryCodeLastGenerated < RecoveryCodeCooldown)
            {
                throw new BusinessException(_localizer["RecoveryCodeCooldown"]);
            }

            var code = GenerateNumericCode(RecoveryCodeLength);
            var expiresAt = DateTime.UtcNow.AddMinutes(RecoveryCodeTtlMinutes);

            user.Account.RecoveryCodeHash = SecureHash.HashSecret(code);
            user.Account.RecoveryCodeExpiresAt = expiresAt;
            user.Account.RecoveryCodeAttempts = 0;
            user.Account.RecoveryCodeLastGenerated = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            if (sendEmail)
            {
                try
                {
                    await _emailService.SendRecoveryCodeAsync(user.Account.Email, code, expiresAt);
                }
                catch (InvalidOperationException)
                {
                    // SMTP não configurado
                    throw new BusinessException(_localizer["EmailNotConfigured"]);
                }
            }

            return (code, expiresAt);
        }

        public async Task ValidateRecoveryCodeAsync(string email, string code)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(email);
            ArgumentException.ThrowIfNullOrWhiteSpace(code);

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                throw new NotFoundException(_localizer["UserNotFoundByEmail", email]);
            }

            if (user.Account.RecoveryCodeHash.IsNullOrEmpty() || !user.Account.RecoveryCodeExpiresAt.HasValue)
            {
                throw new BusinessException(_localizer["RecoveryCodeMissing"]);
            }

            if (user.Account.RecoveryCodeExpiresAt <= DateTime.UtcNow)
            {
                ClearRecoveryCode(user);
                await _userRepository.UpdateAsync(user);
                throw new BusinessException(_localizer["RecoveryCodeExpired"]);
            }

            if (user.Account.RecoveryCodeAttempts >= MaxRecoveryAttempts)
            {
                ClearRecoveryCode(user);
                await _userRepository.UpdateAsync(user);
                throw new BusinessException(_localizer["RecoveryCodeTooManyAttempts"]);
            }

            var isValid = SecureHash.Verify(code, user.Account.RecoveryCodeHash);

            if (!isValid)
            {
                user.Account.RecoveryCodeAttempts++;
                await _userRepository.UpdateAsync(user);
                throw new BusinessException(_localizer["RecoveryCodeInvalid"]);
            }

            await _userRepository.UpdateAsync(user);
        }

        public async Task ChangePasswordWithRecoveryCodeAsync(string email, string code, string newPassword)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(email);
            ArgumentException.ThrowIfNullOrWhiteSpace(code);
            ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                throw new NotFoundException(_localizer["UserNotFoundByEmail", email]);
            }

            if (user.Account.RecoveryCodeHash.IsNullOrEmpty() || !user.Account.RecoveryCodeExpiresAt.HasValue)
            {
                throw new BusinessException(_localizer["RecoveryCodeMissing"]);
            }

            if (user.Account.RecoveryCodeExpiresAt <= DateTime.UtcNow)
            {
                ClearRecoveryCode(user);
                await _userRepository.UpdateAsync(user);
                throw new BusinessException(_localizer["RecoveryCodeExpired"]);
            }

            if (user.Account.RecoveryCodeAttempts >= MaxRecoveryAttempts)
            {
                ClearRecoveryCode(user);
                await _userRepository.UpdateAsync(user);
                throw new BusinessException(_localizer["RecoveryCodeTooManyAttempts"]);
            }

            var isValid = SecureHash.Verify(code, user.Account.RecoveryCodeHash);

            if (!isValid)
            {
                user.Account.RecoveryCodeAttempts++;
                await _userRepository.UpdateAsync(user);
                throw new BusinessException(_localizer["RecoveryCodeInvalid"]);
            }

            _passwordValidator.ValidateAndThrow(new PasswordInput { Password = newPassword });
            user.Account.PasswordHash = SecureHash.HashSecret(newPassword);
            user.Account.RefreshTokenHash = null;
            user.Account.RefreshTokenId = null;
            user.Account.RefreshTokenExpiry = null;
            ClearRecoveryCode(user);

            _userValidator.ValidateAndThrow(user);
            await _userRepository.UpdateAsync(user);
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

        private static void ClearRecoveryCode(User user)
        {
            user.Account.RecoveryCodeHash = null;
            user.Account.RecoveryCodeExpiresAt = null;
            user.Account.RecoveryCodeAttempts = 0;
            user.Account.RecoveryCodeLastGenerated = null;
        }

        private static string GenerateNumericCode(int length)
        {
            var digits = new char[length];
            Span<byte> buffer = stackalloc byte[length];
            RandomNumberGenerator.Fill(buffer);
            for (int i = 0; i < length; i++)
            {
                digits[i] = (char)('0' + (buffer[i] % 10));
            }
            return new string(digits);
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

        private static void EnsurePermissions(User user)
        {
            user.Account.Permissions = NormalizePermissions(user.Account.Permissions);

            if (!user.Account.Permissions.Any())
            {
                user.Account.Permissions = ApplicationPermissions.DefaultUserPermissions.ToList();
            }
        }

        private static List<string> NormalizePermissions(IEnumerable<string>? permissions)
        {
            if (permissions == null)
            {
                return new List<string>();
            }

            return permissions
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
