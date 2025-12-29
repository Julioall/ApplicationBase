using System;
using System.IO;
using Application.Domain.Exceptions;
using Application.Domain.Model;
using Application.Domain.Model.User;
using Application.Service.Interface;
using Application.Service.Service.Security;
using Application.Tests.Setup;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Application.Tests.Services
{
    public class UserServiceTests : BaseTest
    {
        private readonly IUserService _userService;

        public UserServiceTests()
        {
            _userService = _serviceProvider.GetService<IUserService>()
                ?? throw new Exception($"{nameof(IUserService)} não foi encontrado");
        }

        [Fact]
        public async Task GetAll_ShouldReturnListOfUsers()
        {
            // Arrange
            const string email1 = "sam@santos.com";
            const string email2 = "samuel@santos.com";
            const string email3 = "aaa@bbb.com";

            var users = new List<User>
            {
                CreateValidUser(email1),
                CreateValidUser(email2),
                CreateValidUser(email3),
            };

            users.ForEach(_session.Store);
            _session.SaveChanges();

            // Act
            var list = await _userService.GetAllAsync();

            // Assert
            Assert.NotNull(list);
            Assert.Equal(3, list.Count());
            Assert.Contains(list, u => u.Account.Email == email1);
            Assert.Collection(list,
                u => Assert.Equal(email1, u.Account.Email),
                u => Assert.Equal(email2, u.Account.Email),
                u => Assert.Equal(email3, u.Account.Email));
        }

        [Fact]
        public async Task AddAsync_Should_Set_DateJoined_When_Null()
        {
            var user = CreateValidUser("new@user.com", withHash: false);
            user.Account.DateJoined = null;

            await _userService.AddAsync(user, "Valid123!");
            await _asyncSession.SaveChangesAsync();

            var saved = await _userService.GetByEmailAsync(user.Account.Email);
            Assert.NotNull(saved?.Account.DateJoined);
        }

        [Fact]
        public async Task AddAsync_Should_Hash_Password_And_Clear_Plaintext()
        {
            var user = CreateValidUser("hash@user.com", withHash: false);

            await _userService.AddAsync(user, "Valid123!");
            await _asyncSession.SaveChangesAsync();

            var saved = await _userService.GetByEmailAsync(user.Account.Email);
            Assert.NotNull(saved);
            Assert.False(string.IsNullOrWhiteSpace(saved!.Account.PasswordHash));
        }

        [Fact]
        public async Task AddAsync_Should_Throw_When_Duplicate_Email()
        {
            var existing = CreateValidUser("dup@user.com");
            _session.Store(existing);
            _session.SaveChanges();

            var duplicate = CreateValidUser("dup@user.com", withHash: false);

            await Assert.ThrowsAsync<Application.Domain.Exceptions.ConflictException>(() => _userService.AddAsync(duplicate, "Valid123!"));
        }

        [Fact]
        public async Task AddAsync_Should_Throw_When_User_Is_Null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _userService.AddAsync(null!, "Valid123!"));
        }

        [Fact]
        public async Task AddAsync_Should_Throw_When_Password_Invalid()
        {
            var user = CreateValidUser("weak@user.com", withHash: false);
            await Assert.ThrowsAsync<ValidationException>(() => _userService.AddAsync(user, "weak"));
        }

        [Fact]
        public async Task AddAsync_Should_Assign_All_Permissions_To_First_User()
        {
            var user = CreateValidUser("first@user.com", withHash: false);
            user.Account.Permissions = new();

            await _userService.AddAsync(user, "Valid123!");
            await _asyncSession.SaveChangesAsync();

            var saved = await _userService.GetByEmailAsync(user.Account.Email);
            Assert.NotNull(saved);
            Assert.Equal(
                ApplicationPermissions.All.OrderBy(p => p),
                saved!.Account.Permissions.OrderBy(p => p));
        }

        [Fact]
        public async Task AddAsync_Should_Not_Grant_All_Permissions_To_Subsequent_Users()
        {
            var existing = CreateValidUser("existing@user.com");
            _session.Store(existing);
            _session.SaveChanges();

            var user = CreateValidUser("second@user.com", withHash: false);
            user.Account.Permissions = new() { "view:home" };

            await _userService.AddAsync(user, "Valid123!");
            await _asyncSession.SaveChangesAsync();

            var saved = await _userService.GetByEmailAsync(user.Account.Email);
            Assert.NotNull(saved);
            Assert.Equal(new[] { "view:home" }, saved!.Account.Permissions);
            Assert.DoesNotContain(ApplicationPermissions.ManageUsers, saved.Account.Permissions);
        }

        [Fact]
        public async Task UpdateAsync_Should_Throw_When_User_Is_Null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _userService.UpdateAsync(null!));
        }

        [Fact]
        public async Task UpdateAsync_Should_Validate_And_Update()
        {
            var existing = CreateValidUser("existing@user.com");
            _session.Store(existing);
            _session.SaveChanges();

            existing.Profile.Name = "Updated Name";

            await _userService.UpdateAsync(existing);
            await _asyncSession.SaveChangesAsync();

            var updated = await _userService.GetByEmailAsync(existing.Account.Email);
            Assert.Equal("Updated Name", updated?.Profile.Name);
        }

        [Fact]
        public async Task UpdateAsync_Should_Throw_When_Invalid()
        {
            var existing = CreateValidUser("invalid@user.com");
            _session.Store(existing);
            _session.SaveChanges();

            existing.Profile.Name = "";

            await Assert.ThrowsAsync<ValidationException>(() => _userService.UpdateAsync(existing));
        }

        [Fact]
        public async Task DeleteAsync_Should_Remove_User()
        {
            var existing = CreateValidUser("delete@user.com");
            _session.Store(existing);
            _session.SaveChanges();

            await _userService.DeleteAsync(existing.Id);
            await _asyncSession.SaveChangesAsync();

            var missing = await _userService.GetByIdAsync(existing.Id);
            Assert.Null(missing);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_User()
        {
            var existing = CreateValidUser("byid@user.com");
            _session.Store(existing);
            _session.SaveChanges();

            var result = await _userService.GetByIdAsync(existing.Id);

            Assert.NotNull(result);
            Assert.Equal(existing.Id, result!.Id);
        }

        [Fact]
        public async Task GetByEmailAsync_Should_Return_User()
        {
            var existing = CreateValidUser("bye@mail.com");
            _session.Store(existing);
            _session.SaveChanges();

            var result = await _userService.GetByEmailAsync(existing.Account.Email);

            Assert.NotNull(result);
            Assert.Equal(existing.Account.Email, result!.Account.Email);
        }

        [Fact]
        public async Task GetByEmailAsync_Should_Return_Correct_User_When_Multiple_Exist()
        {
            var first = CreateValidUser("first@mail.com");
            var second = CreateValidUser("second@mail.com");
            _session.Store(first);
            _session.Store(second);
            _session.SaveChanges();

            var result = await _userService.GetByEmailAsync(second.Account.Email);

            Assert.NotNull(result);
            Assert.Equal(second.Account.Email, result!.Account.Email);
        }

        [Fact]
        public async Task GetByPermissionAsync_Should_Return_Users()
        {
            var admin1 = CreateValidUser("role@user.com");
            admin1.Account.Permissions = new() { "manage:users" };
            var admin2 = CreateValidUser("role2@user.com");
            admin2.Account.Permissions = new() { "manage:users" };
            _session.Store(admin1);
            _session.Store(admin2);
            _session.SaveChanges();

            var result = await _userService.GetByPermissionAsync("manage:users");

            Assert.NotNull(result);
            Assert.Equal(2, result!.Count());
            Assert.All(result, u => Assert.Contains("manage:users", u.Account.Permissions));
        }

        [Fact]
        public async Task GetByPermissionAsync_Should_Return_Empty_When_No_Users()
        {
            var result = await _userService.GetByPermissionAsync("missing:permission");

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task UpdateProfileAsync_Should_Update_Profile_Fields()
        {
            var existing = CreateValidUser("profile@user.com");
            _session.Store(existing);
            _session.SaveChanges();

            var newDate = new DateTime(1992, 5, 10);
            const string newName = "Updated Profile";
            var imageBytes = new byte[] { 1, 2, 3, 4 };

            await using var stream = new MemoryStream(imageBytes);

            const double offsetX = 10;
            const double offsetY = -5;

            const string jobTitle = "Lead Engineer";
            const string department = "R&D";
            const string organization = "ApplicationBase";
            const string location = "Goiânia";

            const double scale = 1.3;

            await _userService.UpdateProfileAsync(existing.Account.Email, newName, newDate, stream, "image/png", false, offsetX, offsetY, jobTitle, department, organization, location, scale);
            await _asyncSession.SaveChangesAsync();

            var updated = await _userService.GetByEmailAsync(existing.Account.Email);
            Assert.NotNull(updated);
            Assert.Equal(newName, updated!.Profile.Name);
            Assert.Equal(newDate, updated.Profile.DateOfBirth);
            Assert.Equal(offsetX, updated.Profile.ProfilePictureOffsetX);
            Assert.Equal(offsetY, updated.Profile.ProfilePictureOffsetY);
            Assert.Equal(jobTitle, updated.Profile.JobTitle);
            Assert.Equal(department, updated.Profile.Department);
            Assert.Equal(organization, updated.Profile.Organization);
            Assert.Equal(location, updated.Profile.Location);
            Assert.Equal(scale, updated.Profile.ProfilePictureScale);
            Assert.Null(updated.Profile.ProfilePictureUrl);

            var attachment = await _userService.GetProfilePictureAsync(updated.Id);
            Assert.NotNull(attachment);
            Assert.Equal("image/png", attachment!.Value.ContentType);
            Assert.Equal(imageBytes, attachment.Value.Data);
        }

        [Fact]
        public async Task ChangePasswordAsync_Should_Update_When_CurrentPassword_Is_Correct()
        {
            var existing = CreateValidUser("changepass@user.com");
            existing.Account.PasswordHash = SecureHash.HashSecret("OldPass123!");
            _session.Store(existing);
            _session.SaveChanges();

            await _userService.ChangePasswordAsync(existing.Account.Email, "OldPass123!", "NewPass123!");
            await _asyncSession.SaveChangesAsync();

            var updated = await _userService.GetByEmailAsync(existing.Account.Email);
            Assert.NotNull(updated);
            Assert.False(string.IsNullOrWhiteSpace(updated.Account.PasswordHash));
            Assert.Null(updated.Account.RefreshTokenHash);
            Assert.Null(updated.Account.RefreshTokenId);
            Assert.Null(updated.Account.RefreshTokenExpiry);
        }

        [Fact]
        public async Task ChangePasswordAsync_Should_Throw_When_CurrentPassword_Invalid()
        {
            var existing = CreateValidUser("wrongpass@user.com");
            existing.Account.PasswordHash = SecureHash.HashSecret("Correct123!");
            _session.Store(existing);
            _session.SaveChanges();

            await Assert.ThrowsAsync<BusinessException>(() =>
                _userService.ChangePasswordAsync(existing.Account.Email, "Wrong123!", "Another123!"));
        }

        [Fact]
        public async Task GenerateRecoveryCode_Should_Throw_When_In_Cooldown()
        {
            var existing = CreateValidUser("cooldown@test.com");
            existing.Account.RecoveryCodeLastGenerated = DateTime.UtcNow;
            _session.Store(existing);
            _session.SaveChanges();

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _userService.GenerateRecoveryCodeAsync(existing.Account.Email, sendEmail: false));
            Assert.Contains("RecoveryCodeCooldown", ex.Message);
        }

        [Fact]
        public async Task ValidateRecoveryCode_Should_Clear_When_Expired()
        {
            const string code = "123456";
            var existing = CreateValidUser("expired@test.com");
            existing.Account.RecoveryCodeHash = SecureHash.HashSecret(code);
            existing.Account.RecoveryCodeExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            _session.Store(existing);
            _session.SaveChanges();

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _userService.ValidateRecoveryCodeAsync(existing.Account.Email, code));
            Assert.Contains("RecoveryCodeExpired", ex.Message);
            await _asyncSession.SaveChangesAsync();

            var reloaded = await _userService.GetByEmailAsync(existing.Account.Email);
            Assert.NotNull(reloaded);
            Assert.Null(reloaded!.Account.RecoveryCodeHash);
            Assert.Null(reloaded.Account.RecoveryCodeExpiresAt);
        }

        [Fact]
        public async Task ChangePasswordWithRecoveryCode_Should_Reset_RefreshTokens_And_Clear_Code()
        {
            const string code = "654321";
            var existing = CreateValidUser("recover@test.com");
            existing.Account.RecoveryCodeHash = SecureHash.HashSecret(code);
            existing.Account.RecoveryCodeExpiresAt = DateTime.UtcNow.AddMinutes(5);
            existing.Account.RefreshTokenHash = "old-hash";
            existing.Account.RefreshTokenId = "old-id";
            existing.Account.RefreshTokenExpiry = DateTime.UtcNow.AddDays(1);
            _session.Store(existing);
            _session.SaveChanges();

            await _userService.ChangePasswordWithRecoveryCodeAsync(existing.Account.Email, code, "NewPass123!");
            await _asyncSession.SaveChangesAsync();

            var updated = await _userService.GetByEmailAsync(existing.Account.Email);
            Assert.NotNull(updated);
            Assert.False(SecureHash.Verify("Valid123!", updated!.Account.PasswordHash));
            Assert.Null(updated.Account.RefreshTokenHash);
            Assert.Null(updated.Account.RefreshTokenId);
            Assert.Null(updated.Account.RefreshTokenExpiry);
            Assert.Null(updated.Account.RecoveryCodeHash);
            Assert.Null(updated.Account.RecoveryCodeExpiresAt);
        }

        private static User CreateValidUser(string email, bool withHash = true)
        {
            const string defaultPassword = "Valid123!";
            return new User
            {
                Account = new UserAccount
                {
                    Email = email,
                    PasswordHash = withHash ? SecureHash.HashSecret(defaultPassword) : null,
                    Permissions = new() { "view:home", "view:profile" }
                },
                Profile = new UserProfile
                {
                    Name = "Valid Name",
                },
            };
        }
    }
}
