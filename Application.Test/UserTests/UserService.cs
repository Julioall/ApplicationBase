using Application.Domain.Model.User;
using Raven.Client.Documents;
using Application.Service.Interface;
using Application.Test.Setup;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Test.UserTests
{
    public class UserServiceTests : TestHelper
    {
        private readonly IUserService _userService;
        private readonly IDocumentStore _documentStore;

        public UserServiceTests() : base()
        {
            _userService = ServiceProvider.GetRequiredService<IUserService>();
            _documentStore = ServiceProvider.GetRequiredService<IDocumentStore>();
        }

        [Fact]
        public async Task AddAsync_ShouldAddUser()
        {
            // Arrange
            var user = CreateUser("1", "test@example.com");

            // Act
            await _userService.AddAsync(user);

            // Assert
            var retrievedUser = await GetUserFromDatabase("1");
            Assert.NotNull(retrievedUser);
            Assert.Equal(user.Account.Email, retrievedUser.Account.Email);
        }

        [Fact]
        public async Task GetByEmailAsync_ShouldReturnUser()
        {
            // Arrange
            var email = "test@example.com";
            var user = CreateUser("2", email);
            await _userService.AddAsync(user);

            // Act
            var result = await _userService.GetByEmailAsync(email);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(email, result.Account.Email);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateUser()
        {
            // Arrange
            var user = CreateUser("3", "test@example.com");
            await _userService.AddAsync(user);
            user.Profile.Name = "Updated User";

            // Act
            await _userService.UpdateAsync(user);

            // Assert
            var updatedUser = await GetUserFromDatabase("3");
            Assert.Equal("Updated User", updatedUser.Profile.Name);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveUser()
        {
            // Arrange
            var userId = "4";
            var user = CreateUser(userId, "test@example.com");
            await _userService.AddAsync(user);

            // Act
            await _userService.DeleteAsync(userId);

            // Assert
            var deletedUser = await GetUserFromDatabase(userId);
            Assert.Null(deletedUser);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnUser()
        {
            // Arrange
            var userId = "5";
            var user = CreateUser(userId, "test@example.com");
            await _userService.AddAsync(user);

            // Act
            var result = await _userService.GetByIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.Id);
        }

        private User CreateUser(string id, string email)
        {
            return new User
            {
                Id = id,
                Account = new UserAccount
                {
                    Email = email,
                    Password = "securepassword",
                    Role = "User"
                },
                Profile = new UserProfile
                {
                    Name = "Test User"
                }
            };
        }

        private async Task<User> GetUserFromDatabase(string userId)
        {
            using (var session = _documentStore.OpenAsyncSession())
            {
                return await session.LoadAsync<User>(userId);
            }
        }
    }
}
