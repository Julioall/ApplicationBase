using System;
using System.IO;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.User;
using Application.Service.Interface;
using Application.Service.Service;
using Application.Service.Service.Security;
using Microsoft.Extensions.Logging.Abstractions;
using System.Linq;
using Application.Domain.Model;
using Application.Domain.Exceptions;
using Microsoft.Extensions.Localization;
using System.Collections.Generic;
using System.Globalization;
using Application.Service.Service.Moodle;
using Application.Domain.Localization;

namespace Application.Tests.Services
{
    public class TokenServiceTests
    {
        public TokenServiceTests()
        {
            Environment.SetEnvironmentVariable("JWT_ISSUER", "test-issuer");
            Environment.SetEnvironmentVariable("JWT_AUDIENCE", "test-audience");
            Environment.SetEnvironmentVariable("JWT_SIGNING_KEY", "test-signing-key-1234567890-abcdef");
        }

        private sealed class InMemoryUserService : IUserService
        {
            private readonly User _user;

            public InMemoryUserService(User user)
            {
                _user = user;
            }

            public Task AddAsync(User user, string password) => Task.CompletedTask;
            public Task DeleteAsync(string id) => Task.CompletedTask;
            public Task<IEnumerable<User>> GetAllAsync() => Task.FromResult<IEnumerable<User>>(new[] { _user });
            public Task<User> GetByEmailAsync(string email) => Task.FromResult(email == _user.Account.Email ? _user : null)!;
            public Task<User> GetByIdAsync(string id) => Task.FromResult(id == _user.Id ? _user : null)!;
            public Task<User> GetByRefreshTokenAsync(string refreshTokenId) => Task.FromResult(refreshTokenId == _user.Account.RefreshTokenId ? _user : null)!;
            public Task<IEnumerable<User>> GetByPermissionAsync(string permission) => Task.FromResult<IEnumerable<User>>(_user.Account.Permissions.Contains(permission) ? new[] { _user } : Array.Empty<User>());
            public Task UpdateAsync(User user)
            {
                _user.Account = user.Account;
                _user.Profile = user.Profile;
                _user.Id = user.Id;
                return Task.CompletedTask;
            }

            public Task UpdateProfileAsync(string email, string? name, Stream? profilePictureStream, string? profilePictureContentType, bool removeProfilePicture, double? profilePictureOffsetX, double? profilePictureOffsetY, string? jobTitle, string? department, string? organization, string? location, double? profilePictureScale) => Task.CompletedTask;
            public Task ChangePasswordAsync(string email, string currentPassword, string newPassword) => Task.CompletedTask;
            public Task<(byte[] Data, string ContentType)?> GetProfilePictureAsync(string userId) => Task.FromResult<(byte[] Data, string ContentType)?>(null);
            public Task<(string Code, DateTime ExpiresAt)> GenerateRecoveryCodeAsync(string email, bool sendEmail) => Task.FromResult((string.Empty, DateTime.UtcNow.AddMinutes(10)));
            public Task ValidateRecoveryCodeAsync(string email, string code) => Task.CompletedTask;
            public Task ChangePasswordWithRecoveryCodeAsync(string email, string code, string newPassword) => Task.CompletedTask;
        }

        private sealed class FakeLocalizer : IStringLocalizer<SharedResource>
        {
            public LocalizedString this[string name] => new(name, name);

            public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(CultureInfo.InvariantCulture, name, arguments));

            public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();

            public IStringLocalizer WithCulture(CultureInfo culture) => this;
        }

        private sealed class FakeMoodleAuthClient : IMoodleAuthClient
        {
            public Task<string?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
            public Task<MoodleSiteInfo?> GetSiteInfoAsync(string token, CancellationToken cancellationToken = default) => Task.FromResult<MoodleSiteInfo?>(null);
        }

        private sealed class FakeSessionTokenCache : Application.Shared.Session.ISessionTokenCache
        {
            public void SetMoodleToken(int moodleUserId, string token, TimeSpan ttl) { }
            public string? GetMoodleToken(int moodleUserId) => null;
        }

        private static TokenService CreateService(User user)
        {
            return new TokenService(
                new InMemoryUserService(user),
                NullLogger<TokenService>.Instance,
                new FakeLocalizer(),
                new FakeMoodleAuthClient(),
                new FakeSessionTokenCache()
            );
        }

        [Fact]
        public async Task GenerateTokens_Should_Return_Tokens_When_Password_Is_Valid()
        {
            var user = CreateUser();
            var service = CreateService(user);
            var loginDto = new LoginDto { Email = user.Account.Email, Password = "Valid123!" };

            var tokens = await service.GenerateTokens(loginDto);

            Assert.NotNull(tokens);
            Assert.False(string.IsNullOrWhiteSpace(tokens!.Token));
            Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
            Assert.False(string.IsNullOrWhiteSpace(user.Account.RefreshTokenHash));
            Assert.False(string.IsNullOrWhiteSpace(user.Account.RefreshTokenId));
        }

        [Fact]
        public async Task GenerateTokens_Should_Return_Null_When_Password_Is_Invalid()
        {
            var user = CreateUser();
            var service = CreateService(user);
            var loginDto = new LoginDto { Email = user.Account.Email, Password = "Wrong!" };

            var tokens = await service.GenerateTokens(loginDto);

            Assert.Null(tokens);
        }

        [Fact]
        public async Task RefreshAsync_Should_Rotate_RefreshToken_When_Valid()
        {
            var user = CreateUser();
            var service = CreateService(user);
            var loginDto = new LoginDto { Email = user.Account.Email, Password = "Valid123!" };

            var initial = await service.GenerateTokens(loginDto);
            Assert.NotNull(initial);

            var refreshed = await service.RefreshAsync(initial!.RefreshToken);

            Assert.NotNull(refreshed);
            Assert.NotEqual(initial.RefreshToken, refreshed!.RefreshToken);
            Assert.False(string.IsNullOrWhiteSpace(user.Account.RefreshTokenHash));
            Assert.False(string.IsNullOrWhiteSpace(user.Account.RefreshTokenId));
        }

        [Fact]
        public async Task RefreshAsync_Should_Return_Null_For_Invalid_Format()
        {
            var user = CreateUser();
            var service = CreateService(user);

            var refreshed = await service.RefreshAsync("invalid-token");

            Assert.Null(refreshed);
        }

        [Fact]
        public async Task RefreshAsync_Should_Return_Null_When_User_Not_Found()
        {
            var user = CreateUser();
            var service = CreateService(user);
            var loginDto = new LoginDto { Email = user.Account.Email, Password = "Valid123!" };

            var initial = await service.GenerateTokens(loginDto);
            Assert.NotNull(initial);

            var refreshed = await service.RefreshAsync("other-id." + Guid.NewGuid().ToString("N"));

            Assert.Null(refreshed);
        }

        [Fact]
        public async Task RefreshAsync_Should_Return_Null_When_Hash_Mismatch()
        {
            var user = CreateUser();
            var service = CreateService(user);
            var loginDto = new LoginDto { Email = user.Account.Email, Password = "Valid123!" };

            var initial = await service.GenerateTokens(loginDto);
            Assert.NotNull(initial);

            // Corrompe hash para simular token inválido
            user.Account.RefreshTokenHash = SecureHash.HashSecret("other-secret");

            var refreshed = await service.RefreshAsync(initial!.RefreshToken);

            Assert.Null(refreshed);
        }

        [Fact]
        public async Task GenerateTokens_Should_Return_Null_When_LoginDto_Is_Null()
        {
            var user = CreateUser();
            var service = CreateService(user);

            var tokens = await service.GenerateTokens(null!);

            Assert.Null(tokens);
        }

        [Fact]
        public async Task GenerateTokens_Should_Throw_When_SigningKey_Missing()
        {
            var user = CreateUser();
            var service = CreateService(user);
            var loginDto = new LoginDto { Email = user.Account.Email, Password = "Valid123!" };

            var previousKey = Environment.GetEnvironmentVariable(ApplicationConstants.JWT_SIGNING_KEY);
            try
            {
                Environment.SetEnvironmentVariable(ApplicationConstants.JWT_SIGNING_KEY, null);

                await Assert.ThrowsAsync<ConfigurationException>(() => service.GenerateTokens(loginDto));
            }
            finally
            {
                Environment.SetEnvironmentVariable(ApplicationConstants.JWT_SIGNING_KEY, previousKey);
            }
        }

        [Fact]
        public async Task RefreshAsync_Should_Return_Null_When_Token_Expired()
        {
            var user = CreateUser();
            var service = CreateService(user);
            var loginDto = new LoginDto { Email = user.Account.Email, Password = "Valid123!" };

            var initial = await service.GenerateTokens(loginDto);
            Assert.NotNull(initial);

            user.Account.RefreshTokenExpiry = DateTime.UtcNow.AddMinutes(-1);

            var refreshed = await service.RefreshAsync(initial!.RefreshToken);
            Assert.Null(refreshed);
        }

        [Fact]
        public async Task RefreshAsync_Should_Return_Null_When_Expiry_Null()
        {
            var user = CreateUser();
            var service = CreateService(user);
            var loginDto = new LoginDto { Email = user.Account.Email, Password = "Valid123!" };

            var initial = await service.GenerateTokens(loginDto);
            Assert.NotNull(initial);

            user.Account.RefreshTokenExpiry = null;

            var refreshed = await service.RefreshAsync(initial!.RefreshToken);
            Assert.Null(refreshed);
        }

        private static User CreateUser()
        {
            const string password = "Valid123!";
            return new User
            {
                Id = Guid.NewGuid().ToString("N"),
                Account = new UserAccount
                {
                    Email = "user@test.com",
                    PasswordHash = SecureHash.HashSecret(password),
                    Permissions = new() { "view:home", "view:profile" }
                },
                Profile = new UserProfile
                {
                    Name = "Tester"
                }
            };
        }
    }
}
