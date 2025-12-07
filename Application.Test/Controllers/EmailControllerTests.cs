using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using Application.Api.Controllers;
using Application.Domain;
using Application.Domain.Model;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.User;
using Application.Service.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Application.Tests.Controllers
{
    public class EmailControllerTests
    {
        private sealed class FakeLocalizer : IStringLocalizer<SharedResource>
        {
            public LocalizedString this[string name] => new(name, name);
            public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));
            public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
            public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
        }

        private sealed class FakeEmailService : IEmailService
        {
            public bool ResetCalled { get; private set; }
            public bool TestCalled { get; private set; }

            public Task SendPasswordResetAsync(string toEmail)
            {
                ResetCalled = true;
                return Task.CompletedTask;
            }

            public Task SendRecoveryCodeAsync(string toEmail, string code, DateTime expiresAt) => Task.CompletedTask;

            public Task SendTestEmailAsync(string toEmail, EmailSettings? overrideSettings = null)
            {
                TestCalled = true;
                return Task.CompletedTask;
            }
        }

        private sealed class FakeUserService : IUserService
        {
            public Func<string, Task<User?>>? GetByEmailFunc { get; set; }

            public Task AddAsync(User user, string password) => Task.CompletedTask;
            public Task DeleteAsync(string id) => Task.CompletedTask;
            public Task<IEnumerable<User>> GetAllAsync() => Task.FromResult<IEnumerable<User>>(Array.Empty<User>());
            public Task<User> GetByEmailAsync(string email) => (GetByEmailFunc?.Invoke(email) ?? Task.FromResult<User?>(null))!;
            public Task<User> GetByIdAsync(string id) => Task.FromResult<User>(null!);
            public Task<IEnumerable<User>> GetByPermissionAsync(string permission) => Task.FromResult<IEnumerable<User>>(Array.Empty<User>());
            public Task<User> GetByRefreshTokenAsync(string refreshTokenId) => Task.FromResult<User>(null!);
            public Task UpdateAsync(User user) => Task.CompletedTask;
            public Task UpdateProfileAsync(string email, string? name, DateTime? dateOfBirth, Stream? profilePictureStream, string? profilePictureContentType, bool removeProfilePicture, double? profilePictureOffsetX, double? profilePictureOffsetY, string? jobTitle, string? department, string? organization, string? location, double? profilePictureScale) => Task.CompletedTask;
            public Task ChangePasswordAsync(string email, string currentPassword, string newPassword) => Task.CompletedTask;
            public Task<(byte[] Data, string ContentType)?> GetProfilePictureAsync(string userId) => Task.FromResult<(byte[] Data, string ContentType)?>(null);
            public Task<(string Code, DateTime ExpiresAt)> GenerateRecoveryCodeAsync(string email, bool sendEmail) => Task.FromResult((string.Empty, DateTime.UtcNow));
            public Task ValidateRecoveryCodeAsync(string email, string code) => Task.CompletedTask;
            public Task ChangePasswordWithRecoveryCodeAsync(string email, string code, string newPassword) => Task.CompletedTask;
        }

        private sealed class FakeSettingsService : ISettingsService
        {
            public EmailSettings Current { get; set; } = new EmailSettings();
            public EmailSettings? Saved { get; private set; }

            public Task<Configurations> GetOrCreateAsync() => Task.FromResult(new Configurations { Email = Current });
            public Task<EmailSettings> GetEmailAsync() => Task.FromResult(Current);
            public Task<EmailSettings> SaveEmailAsync(EmailSettings settings)
            {
                Saved = settings;
                return Task.FromResult(settings);
            }
        }

        private static EmailController BuildController(FakeEmailService? emailService = null, FakeUserService? userService = null, FakeSettingsService? settingsService = null, EmailSettings? options = null, ClaimsPrincipal? user = null)
        {
            emailService ??= new FakeEmailService();
            userService ??= new FakeUserService();
            settingsService ??= new FakeSettingsService();
            options ??= new EmailSettings();

            var controller = new EmailController(emailService, userService, new FakeLocalizer(), Options.Create(options), settingsService);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user ?? new ClaimsPrincipal(new ClaimsIdentity())
                }
            };
            return controller;
        }

        [Fact]
        public async Task SendReset_Should_Return_BadRequest_When_Email_Missing_And_No_Claim()
        {
            var controller = BuildController();

            var result = await controller.SendReset(null!) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status400BadRequest, result!.StatusCode);
            var problem = Assert.IsType<ProblemDetails>(result.Value);
            Assert.Equal("InvalidRequestTitle", problem.Title);
            Assert.Equal("EmailRequired", problem.Detail);
        }

        [Fact]
        public async Task SendReset_Should_Invoke_EmailService_When_User_Exists()
        {
            var emailService = new FakeEmailService();
            var userService = new FakeUserService
            {
                GetByEmailFunc = _ => Task.FromResult<User?>(new User { Account = new UserAccount { Email = "user@test.com" } })
            };

            var controller = BuildController(emailService, userService);
            var result = await controller.SendReset(new SendResetEmailDto { Email = "user@test.com" }) as OkObjectResult;

            Assert.NotNull(result);
            Assert.True(emailService.ResetCalled);
            Assert.Equal(StatusCodes.Status200OK, result!.StatusCode);
        }

        [Fact]
        public async Task UpdateSettings_Should_Return_BadRequest_When_Body_Is_Null()
        {
            var controller = BuildController();

            var result = await controller.UpdateSettings(null!) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status400BadRequest, result!.StatusCode);
            var problem = Assert.IsType<ProblemDetails>(result.Value);
            Assert.Equal("InvalidRequestTitle", problem.Title);
            Assert.Equal("InvalidOperationTitle", problem.Detail);
        }

        [Fact]
        public async Task UpdateSettings_Should_Return_BadRequest_When_Required_Fields_Missing()
        {
            var controller = BuildController();
            var invalid = new EmailSettings
            {
                Host = "",
                FromEmail = "",
                Password = "",
                Port = 0
            };

            var result = await controller.UpdateSettings(invalid) as ObjectResult;

            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status400BadRequest, result!.StatusCode);
            var problem = Assert.IsType<ProblemDetails>(result.Value);
            Assert.Equal("EmailSettingsFieldsRequired", problem.Detail);
        }

        [Fact]
        public async Task UpdateSettings_Should_Reuse_Current_Password_When_Empty()
        {
            var settingsService = new FakeSettingsService
            {
                Current = new EmailSettings
                {
                    Host = "smtp.test",
                    FromEmail = "from@test.com",
                    Password = "current-secret",
                    Port = 25
                }
            };

            var controller = BuildController(settingsService: settingsService);
            var incoming = new EmailSettings
            {
                Host = "smtp.test",
                FromEmail = "from@test.com",
                Password = "",
                Port = 25
            };

            var result = await controller.UpdateSettings(incoming) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(StatusCodes.Status200OK, result!.StatusCode);
            Assert.NotNull(settingsService.Saved);
            Assert.Equal("current-secret", settingsService.Saved!.Password);
        }
    }
}
