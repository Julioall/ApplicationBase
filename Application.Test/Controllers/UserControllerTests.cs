using Application.Api.Controllers;
using Application.Domain;
using Application.Domain.Exceptions;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.User;
using Application.Service.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Application.Tests.Controllers
{
    public class UserControllerTests
    {
        private sealed class FakeLocalizer : IStringLocalizer<SharedResource>
        {
            public LocalizedString this[string name] => new(name, name);
            public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));
            public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
            public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
        }

        private sealed class FakeUserService : IUserService
        {
            public Func<User, string, Task>? AddFunc { get; set; }
            public Func<string, Task>? DeleteFunc { get; set; }
            public Func<Task<IEnumerable<User>>>? GetAllFunc { get; set; }
            public Func<string, Task<User?>>? GetByIdFunc { get; set; }
            public Func<string, Task<User?>>? GetByEmailFunc { get; set; }
            public Func<string, Task<IEnumerable<User>>?>? GetByRoleFunc { get; set; }
            public Func<string, Task<User?>>? GetByRefreshTokenFunc { get; set; }
            public Func<User, Task>? UpdateFunc { get; set; }
            public Func<string, Task<(byte[] Data, string ContentType)?>>? GetProfilePictureFunc { get; set; }
            public Func<string, string?, DateTime?, Stream?, string?, bool, double?, double?, string?, string?, string?, string?, double?, Task>? UpdateProfileFunc { get; set; }
            public Func<string, string, string, Task>? ChangePasswordFunc { get; set; }

            public Task AddAsync(User user, string password) => AddFunc?.Invoke(user, password) ?? Task.CompletedTask;
            public Task DeleteAsync(string id) => DeleteFunc?.Invoke(id) ?? Task.CompletedTask;
            public Task<IEnumerable<User>> GetAllAsync() => GetAllFunc?.Invoke() ?? Task.FromResult<IEnumerable<User>>(Array.Empty<User>());
            public async Task<User> GetByIdAsync(string id) => (await (GetByIdFunc?.Invoke(id) ?? Task.FromResult<User?>(null)))!;
            public async Task<User> GetByEmailAsync(string email) => (await (GetByEmailFunc?.Invoke(email) ?? Task.FromResult<User?>(null)))!;
            public Task<IEnumerable<User>> GetByRoleAsync(string role) => GetByRoleFunc?.Invoke(role) ?? Task.FromResult<IEnumerable<User>>(Array.Empty<User>());
            public async Task<User> GetByRefreshTokenAsync(string refreshToken) => (await (GetByRefreshTokenFunc?.Invoke(refreshToken) ?? Task.FromResult<User?>(null)))!;
            public Task UpdateAsync(User user) => UpdateFunc?.Invoke(user) ?? Task.CompletedTask;
            public Task UpdateProfileAsync(string email, string? name, DateTime? dateOfBirth, Stream? profilePictureStream, string? profilePictureContentType, bool removeProfilePicture, double? profilePictureOffsetX, double? profilePictureOffsetY, string? jobTitle, string? department, string? organization, string? location, double? profilePictureScale) => UpdateProfileFunc?.Invoke(email, name, dateOfBirth, profilePictureStream, profilePictureContentType, removeProfilePicture, profilePictureOffsetX, profilePictureOffsetY, jobTitle, department, organization, location, profilePictureScale) ?? Task.CompletedTask;
            public Task ChangePasswordAsync(string email, string currentPassword, string newPassword) => ChangePasswordFunc?.Invoke(email, currentPassword, newPassword) ?? Task.CompletedTask;
            public Task<(byte[] Data, string ContentType)?> GetProfilePictureAsync(string userId) => GetProfilePictureFunc?.Invoke(userId) ?? Task.FromResult<(byte[] Data, string ContentType)?>(null);
        }

        private static User CreateUser(string id = "1") => new User
        {
            Id = id,
            Account = new UserAccount { Email = "mail@test.com", PasswordHash = "hash", Role = "User" },
            Profile = new UserProfile { Name = "Tester" }
        };

        private static CreateUserDto CreateUserRequest(string? name = null, string? email = null, string password = "Valid123!", string role = "User")
        {
            return new CreateUserDto
            {
                Account = new CreateUserAccountDto
                {
                    Email = email ?? "mail@test.com",
                    Password = password,
                    Role = role
                },
                Profile = new CreateUserProfileDto
                {
                    Name = name ?? "Tester",
                    DateOfBirth = null,
                    ProfilePictureUrl = null,
                    Department = null,
                    JobTitle = null,
                    Organization = null,
                    Location = null
                }
            };
        }

        [Fact]
        public async Task AddUser_Should_Return_BadRequest_When_Body_Is_Null()
        {
            var controller = new UserController(new FakeUserService(), new FakeLocalizer());

            var result = await controller.AddUser(null!) as ObjectResult;

            Assert.NotNull(result);
            var problem = Assert.IsType<ProblemDetails>(result!.Value);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            Assert.Equal("InvalidRequestTitle", problem.Title);
            Assert.Equal("UserCannotBeNullDetail", problem.Detail);
        }

        [Fact]
        public async Task AddUser_Should_Return_BadRequest_When_Password_Missing()
        {
            var controller = new UserController(new FakeUserService(), new FakeLocalizer());
            var dto = CreateUserRequest(password: "");

            var result = await controller.AddUser(dto) as ObjectResult;

            Assert.NotNull(result);
            var problem = Assert.IsType<ProblemDetails>(result!.Value);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            Assert.Equal("InvalidRequestTitle", problem.Title);
            Assert.Equal("PasswordRequired", problem.Detail);
        }

        [Fact]
        public async Task AddUser_Should_Return_Created_With_Message()
        {
            var service = new FakeUserService
            {
                AddFunc = (u, _) =>
                {
                    u.Id ??= "123";
                    return Task.CompletedTask;
                }
            };
            var controller = new UserController(service, new FakeLocalizer());
            var dto = CreateUserRequest();

            var result = await controller.AddUser(dto) as CreatedAtActionResult;

            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status201Created, result!.StatusCode);
            var payload = result.Value!;
            Assert.Equal("UserAddedSuccessfully", payload.GetType().GetProperty("message")?.GetValue(payload)?.ToString());
        }

        [Fact]
        public async Task DeleteUser_Should_Throw_NotFound_When_Missing()
        {
            var service = new FakeUserService
            {
                GetByIdFunc = _ => Task.FromResult<User?>(null)
            };
            var controller = new UserController(service, new FakeLocalizer());

            await Assert.ThrowsAsync<NotFoundException>(() => controller.DeleteUser("999"));
        }

        [Fact]
        public async Task DeleteUser_Should_Return_NoContent_When_Deleted()
        {
            var deleted = false;

            var service = new FakeUserService
            {
                GetByIdFunc = _ => Task.FromResult<User?>(CreateUser("2")),
                DeleteFunc = _ => { deleted = true; return Task.CompletedTask; }
            };
            var controller = new UserController(service, new FakeLocalizer());

            var result = await controller.DeleteUser("2") as NoContentResult;

            Assert.True(deleted);
            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status204NoContent, result!.StatusCode);
        }

        [Fact]
        public async Task GetAllUsers_Should_Return_Ok_With_List()
        {
            var service = new FakeUserService
            {
                GetAllFunc = () => Task.FromResult<IEnumerable<User>>(new[] { CreateUser("a"), CreateUser("b") })
            };
            var controller = new UserController(service, new FakeLocalizer());

            var actionResult = await controller.GetAllUsers();
            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            var users = Assert.IsAssignableFrom<IEnumerable<User>>(ok.Value);
            Assert.Equal(2, users.Count());
        }

        [Fact]
        public async Task GetUserById_Should_Return_Ok_When_Found()
        {
            var service = new FakeUserService
            {
                GetByIdFunc = _ => Task.FromResult<User?>(CreateUser("5"))
            };
            var controller = new UserController(service, new FakeLocalizer());

            var actionResult = await controller.GetUserById("5");
            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            var user = Assert.IsType<User>(ok.Value);
            Assert.Equal("5", user.Id);
        }

        [Fact]
        public async Task GetUserById_Should_Throw_NotFound_When_Missing()
        {
            var service = new FakeUserService
            {
                GetByIdFunc = _ => Task.FromResult<User?>(null)
            };
            var controller = new UserController(service, new FakeLocalizer());

            await Assert.ThrowsAsync<NotFoundException>(() => controller.GetUserById("missing"));
        }

        [Fact]
        public async Task GetUserByEmail_Should_Return_Ok_When_Found()
        {
            string? receivedEmail = null;
            var service = new FakeUserService
            {
                GetByEmailFunc = email =>
                {
                    receivedEmail = email;
                    return Task.FromResult<User?>(CreateUser("email-id"));
                }
            };
            var controller = new UserController(service, new FakeLocalizer());

            var actionResult = await controller.GetUserByEmail("mail@test.com");
            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            var user = Assert.IsType<User>(ok.Value);
            Assert.Equal("mail@test.com", receivedEmail);
            Assert.Equal("email-id", user.Id);
        }

        [Fact]
        public async Task GetUserByEmail_Should_Throw_NotFound_When_Missing()
        {
            string? receivedEmail = null;
            var service = new FakeUserService
            {
                GetByEmailFunc = email =>
                {
                    receivedEmail = email;
                    return Task.FromResult<User?>(null);
                }
            };
            var controller = new UserController(service, new FakeLocalizer());

            await Assert.ThrowsAsync<NotFoundException>(() => controller.GetUserByEmail("none@test.com"));
            Assert.Equal("none@test.com", receivedEmail);
        }

        [Fact]
        public async Task GetUsersByRole_Should_Return_Ok_With_List()
        {
            var user = CreateUser("role-id");
            user.Account.Role = "Admin";
            var second = CreateUser("role-id-2");
            second.Account.Role = "Admin";
            var service = new FakeUserService
            {
                GetByRoleFunc = _ => Task.FromResult<IEnumerable<User>>(new[] { user, second })
            };
            var controller = new UserController(service, new FakeLocalizer());

            var actionResult = await controller.GetUsersByRole("Admin");
            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            var returnedUsers = Assert.IsAssignableFrom<IEnumerable<User>>(ok.Value);
            Assert.Equal(2, returnedUsers.Count());
            Assert.All(returnedUsers, u => Assert.Equal("Admin", u.Account.Role));
        }

        [Fact]
        public async Task GetUsersByRole_Should_Return_Ok_With_Empty_List_When_NotFound()
        {
            var service = new FakeUserService
            {
                GetByRoleFunc = _ => Task.FromResult<IEnumerable<User>>(Array.Empty<User>())
            };
            var controller = new UserController(service, new FakeLocalizer());

            var actionResult = await controller.GetUsersByRole("Missing");
            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            var users = Assert.IsAssignableFrom<IEnumerable<User>>(ok.Value);
            Assert.Empty(users);
        }

        [Fact]
        public async Task UpdateUser_Should_Return_BadRequest_When_User_Null()
        {
            var controller = new UserController(new FakeUserService(), new FakeLocalizer());

            var result = await controller.UpdateUser(null) as ObjectResult;

            Assert.NotNull(result);
            var problem = Assert.IsType<ProblemDetails>(result!.Value);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            Assert.Equal("InvalidRequestTitle", problem.Title);
            Assert.Equal("UserCannotBeNullDetail", problem.Detail);
        }

        [Fact]
        public async Task UpdateUser_Should_Return_BadRequest_When_Id_Empty()
        {
            var controller = new UserController(new FakeUserService(), new FakeLocalizer());
            var user = CreateUser(string.Empty);

            var result = await controller.UpdateUser(user) as ObjectResult;

            Assert.NotNull(result);
            var problem = Assert.IsType<ProblemDetails>(result!.Value);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            Assert.Equal("InvalidRequestTitle", problem.Title);
            Assert.Equal("UserCannotBeNullDetail", problem.Detail);
        }

        [Fact]
        public async Task UpdateUser_Should_Return_BadRequest_When_Body_Is_Null()
        {
            var controller = new UserController(new FakeUserService(), new FakeLocalizer());

            var result = await controller.UpdateUser(null!) as ObjectResult;

            Assert.NotNull(result);
            var problem = Assert.IsType<ProblemDetails>(result!.Value);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            Assert.Equal("InvalidRequestTitle", problem.Title);
            Assert.Equal("UserCannotBeNullDetail", problem.Detail);
        }

        [Fact]
        public async Task UpdateUser_Should_Throw_NotFound_When_User_Does_Not_Exist()
        {
            var service = new FakeUserService
            {
                GetByIdFunc = _ => Task.FromResult<User?>(null)
            };
            var controller = new UserController(service, new FakeLocalizer());
            var user = CreateUser("missing");

            await Assert.ThrowsAsync<NotFoundException>(() => controller.UpdateUser(user));
        }

        [Fact]
        public async Task UpdateUser_Should_Return_Ok_With_Message()
        {
            var updated = false;

            var service = new FakeUserService
            {
                GetByIdFunc = _ => Task.FromResult<User?>(CreateUser("7")),
                UpdateFunc = _ => { updated = true; return Task.CompletedTask; }
            };
            var controller = new UserController(service, new FakeLocalizer());
            var user = CreateUser("7");

            var result = await controller.UpdateUser(user) as OkObjectResult;

            Assert.True(updated);
            Assert.NotNull(result);
            var payload = result!.Value!;
            Assert.Equal("UserUpdatedSuccessfully", payload.GetType().GetProperty("message")?.GetValue(payload)?.ToString());
        }
    }
}
