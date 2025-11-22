using System;
using System.IO;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.User;
using Application.Service.Interface;
using Application.Service.Service;
using Application.Service.Service.Security;
using Microsoft.Extensions.Logging.Abstractions;

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
            public Task<IEnumerable<User>> GetByRoleAsync(string role) => Task.FromResult<IEnumerable<User>>(role == _user.Account.Role ? new[] { _user } : Array.Empty<User>());
            public Task UpdateAsync(User user)
            {
                _user.Account = user.Account;
                _user.Profile = user.Profile;
                _user.Id = user.Id;
                return Task.CompletedTask;
            }

            public Task UpdateProfileAsync(string email, string? name, DateTime? dateOfBirth, Stream? profilePictureStream, string? profilePictureContentType, bool removeProfilePicture, double? profilePictureOffsetX, double? profilePictureOffsetY, string? jobTitle, string? department, string? organization, string? location, double? profilePictureScale) => Task.CompletedTask;
            public Task ChangePasswordAsync(string email, string currentPassword, string newPassword) => Task.CompletedTask;
            public Task<(byte[] Data, string ContentType)?> GetProfilePictureAsync(string userId) => Task.FromResult<(byte[] Data, string ContentType)?>(null);
        }

        [Fact]
        public async Task GenerateTokens_Should_Return_Tokens_When_Password_Is_Valid()
        {
            var user = CreateUser();
            var service = new TokenService(new InMemoryUserService(user), NullLogger<TokenService>.Instance);
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
            var service = new TokenService(new InMemoryUserService(user), NullLogger<TokenService>.Instance);
            var loginDto = new LoginDto { Email = user.Account.Email, Password = "Wrong!" };

            var tokens = await service.GenerateTokens(loginDto);

            Assert.Null(tokens);
        }

        [Fact]
        public async Task RefreshAsync_Should_Rotate_RefreshToken_When_Valid()
        {
            var user = CreateUser();
            var service = new TokenService(new InMemoryUserService(user), NullLogger<TokenService>.Instance);
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
            var service = new TokenService(new InMemoryUserService(user), NullLogger<TokenService>.Instance);

            var refreshed = await service.RefreshAsync("invalid-token");

            Assert.Null(refreshed);
        }

        [Fact]
        public async Task RefreshAsync_Should_Return_Null_When_Token_Expired()
        {
            var user = CreateUser();
            var service = new TokenService(new InMemoryUserService(user), NullLogger<TokenService>.Instance);
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
            var service = new TokenService(new InMemoryUserService(user), NullLogger<TokenService>.Instance);
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
                    Role = "User"
                },
                Profile = new UserProfile
                {
                    Name = "Tester"
                }
            };
        }
    }
}
