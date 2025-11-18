using Microsoft.Extensions.DependencyInjection;
using Application.Tests.Setup;
using Application.Domain;
using Application.Domain.Model.User;
using Application.Service.Interface;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Tests.Validator
{
    public class UserValidatorTests : BaseTest
    {
        private readonly IValidator<User> _userValidator;
        private readonly IUserService _userService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public UserValidatorTests()
        {
            _userValidator = _serviceProvider.GetService<IValidator<User>>()
                ?? throw new Exception($"{nameof(IValidator<User>)} não foi encontrado");
            _userService = _serviceProvider.GetService<IUserService>()
                ?? throw new Exception($"{nameof(IUserService)} não foi encontrado");
            _localizer = _serviceProvider.GetService<IStringLocalizer<SharedResource>>()
                ?? throw new Exception($"{nameof(IStringLocalizer<SharedResource>)} não foi encontrado");
        }

        [Fact]
        public void Should_Have_Error_When_Email_Is_Empty()
        {
            var user = CreateValidUser();
            user.Account.Email = "";

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains(_localizer["EmailRequired"], result.Message);
            Assert.Contains(_localizer["EmailInvalid"], result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_Email_Is_Invalid()
        {
            var user = CreateValidUser();
            user.Account.Email = "invalid-email";

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains(_localizer["EmailInvalid"], result.Message);
        }

        [Fact]
        public async Task Should_Return_ValidationError_When_Email_Already_Exists()
        {
            var existing = CreateValidUser();
            existing.Account.Email = "existing@email.com";
            _session.Store(existing);
            _session.SaveChanges();

            var duplicate = CreateValidUser();
            duplicate.Account.Email = "existing@email.com";

            var ex = await Assert.ThrowsAsync<ValidationException>(() => _userService.AddAsync(duplicate));
            Assert.Contains(_localizer["EmailAlreadyExists"], ex.Message, StringComparison.InvariantCultureIgnoreCase);
        }

        [Fact]
        public void Should_Have_Error_When_Password_Is_Empty()
        {
            var user = CreateValidUser();
            user.Account.Password = "";

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains(_localizer["PasswordRequired"], result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_Password_Is_Too_Short()
        {
            var user = CreateValidUser();
            user.Account.Password = "Ab1!";

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains(_localizer["PasswordMinLength"], result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_Password_Missing_Uppercase()
        {
            var user = CreateValidUser();
            user.Account.Password = "valid123!";

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains(_localizer["PasswordUppercase"], result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_Password_Missing_Lowercase()
        {
            var user = CreateValidUser();
            user.Account.Password = "VALID123!";

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains(_localizer["PasswordLowercase"], result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_Password_Missing_Number()
        {
            var user = CreateValidUser();
            user.Account.Password = "ValidPass!";

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains(_localizer["PasswordNumber"], result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_Password_Missing_Special_Character()
        {
            var user = CreateValidUser();
            user.Account.Password = "Valid123";

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains(_localizer["PasswordSpecial"], result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_Name_Is_Empty()
        {
            var user = CreateValidUser();
            user.Profile.Name = "";

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains(_localizer["NameRequired"], result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_Name_Exceeds_Max_Length()
        {
            var user = CreateValidUser();
            user.Profile.Name = new string('A', 101);

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains(_localizer["NameMaxLength"], result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_DateOfBirth_Is_Future()
        {
            var user = CreateValidUser();
            user.Profile.DateOfBirth = DateTime.Now.AddDays(1);

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains(_localizer["DateOfBirthPast"], result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_DateOfBirth_Is_Too_Old()
        {
            var user = CreateValidUser();
            user.Profile.DateOfBirth = DateTime.Now.AddYears(-121);

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains(_localizer["DateOfBirthTooOld"], result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_Role_Is_Empty()
        {
            var user = CreateValidUser();
            user.Account.Role = "";

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains(_localizer["RoleRequired"], result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_Role_Is_Invalid()
        {
            var user = CreateValidUser();
            user.Account.Role = "SuperUser";

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains(_localizer["RoleInvalid"], result.Message);
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
                    Password = "Valid123!",
                    Role = "User"
                },
                Profile = new UserProfile
                {
                    Name = "Valid Name",
                    DateOfBirth = DateTime.Now.AddYears(-25)
                },
            };
        }
    }
}
