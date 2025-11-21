using Application.Domain.Model.User;
using Application.Service.Interface;
using Application.Service.Service.Security;
using Application.Tests.Setup;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Tests.Validator
{
    public class UserValidatorTests : BaseTest
    {
        private readonly IValidator<User> _userValidator;
        private readonly IUserService _userService;

        public UserValidatorTests()
        {
            _userValidator = _serviceProvider.GetService<IValidator<User>>()
                ?? throw new Exception($"{nameof(IValidator<User>)} não foi encontrado");
            _userService = _serviceProvider.GetService<IUserService>()
                ?? throw new Exception($"{nameof(IUserService)} não foi encontrado");
        }

        [Fact]
        public void Should_Have_Error_When_Email_Is_Empty()
        {
            var user = CreateValidUser();
            user.Account.Email = "";

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains("EmailRequired", result.Message);
            Assert.Contains("EmailInvalid", result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_Email_Is_Invalid()
        {
            var user = CreateValidUser();
            user.Account.Email = "invalid-email";

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains("EmailInvalid", result.Message);
        }

        [Fact]
        public async Task Should_Return_Error_When_Email_Already_Exists()
        {
            var existing = CreateValidUser();
            existing.Account.Email = "existing@email.com";
            _session.Store(existing);
            _session.SaveChanges();

            var duplicate = CreateValidUser();
            duplicate.Account.Email = "existing@email.com";

            await Assert.ThrowsAsync<Application.Domain.Exceptions.ConflictException>(() => _userService.AddAsync(duplicate, "Valid123!"));
        }

        [Fact]
        public void Should_Have_Error_When_Name_Is_Empty()
        {
            var user = CreateValidUser();
            user.Profile.Name = "";

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains("NameRequired", result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_Name_Exceeds_Max_Length()
        {
            var user = CreateValidUser();
            user.Profile.Name = new string('A', 101);

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains("NameMaxLength", result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_DateOfBirth_Is_Future()
        {
            var user = CreateValidUser();
            user.Profile.DateOfBirth = DateTime.UtcNow.AddDays(1);

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains("DateOfBirthPast", result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_DateOfBirth_Is_Too_Old()
        {
            var user = CreateValidUser();
            user.Profile.DateOfBirth = DateTime.UtcNow.AddYears(-121);

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains("DateOfBirthTooOld", result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_Role_Is_Empty()
        {
            var user = CreateValidUser();
            user.Account.Role = "";

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains("RoleRequired", result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_Role_Is_Invalid()
        {
            var user = CreateValidUser();
            user.Account.Role = "SuperUser";

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains("RoleInvalid", result.Message);
        }

        [Fact]
        public void Should_Pass_Validation_For_Valid_User()
        {
            var user = CreateValidUser();
            user.Account.Email = "sam@araag.com";
            _session.Store(user);
            _session.SaveChanges();

            user.Account.Email = "valid@email.com";
            var result = _userValidator.Validate(user);

            Assert.True(result.IsValid);
        }

        private static User CreateValidUser()
        {
            return new User
            {
                Account = new UserAccount
                {
                    Email = "valid@email.com",
                    PasswordHash = SecureHash.HashSecret("Valid123!"),
                    Role = "User"
                },
                Profile = new UserProfile
                {
                    Name = "Valid Name",
                    DateOfBirth = DateTime.UtcNow.AddYears(-25)
                },
            };
        }
    }
}
