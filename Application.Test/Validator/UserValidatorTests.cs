using Microsoft.Extensions.DependencyInjection;
using Application.Tests.Setup;
using Application.Domain.Model.User;
using Application.Service.Interface;
using Application.Domain.Exceptions;
using FluentValidation;

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

            Assert.Contains("O email é obrigatório.", result.Message);
            Assert.Contains("O email deve ser válido.", result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_Email_Is_Invalid()
        {
            var user = CreateValidUser();
            user.Account.Email = "invalid-email";

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains("O email deve ser válido.", result.Message);
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
            Assert.Contains("já existe", ex.Message, StringComparison.InvariantCultureIgnoreCase);
        }

        [Fact]
        public void Should_Have_Error_When_Password_Is_Empty()
        {
            var user = CreateValidUser();
            user.Account.Password = "";

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains("A senha é obrigatória.", result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_Password_Is_Too_Short()
        {
            var user = CreateValidUser();
            user.Account.Password = "Ab1!";

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains("A senha deve ter pelo menos 8 caracteres.", result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_Password_Missing_Uppercase()
        {
            var user = CreateValidUser();
            user.Account.Password = "valid123!";

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains("A senha deve conter pelo menos uma letra maiúscula.", result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_Password_Missing_Lowercase()
        {
            var user = CreateValidUser();
            user.Account.Password = "VALID123!";

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains("A senha deve conter pelo menos uma letra minúscula.", result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_Password_Missing_Number()
        {
            var user = CreateValidUser();
            user.Account.Password = "ValidPass!";

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains("A senha deve conter pelo menos um número.", result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_Password_Missing_Special_Character()
        {
            var user = CreateValidUser();
            user.Account.Password = "Valid123";

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains("A senha deve conter pelo menos um caractere especial", result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_Name_Is_Empty()
        {
            var user = CreateValidUser();
            user.Profile.Name = "";

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains("O nome é obrigatório.", result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_Name_Exceeds_Max_Length()
        {
            var user = CreateValidUser();
            user.Profile.Name = new string('A', 101);

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains("O nome não pode ter mais de 100 caracteres.", result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_DateOfBirth_Is_Future()
        {
            var user = CreateValidUser();
            user.Profile.DateOfBirth = DateTime.Now.AddDays(1);

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains("A data de nascimento deve ser uma data passada.", result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_DateOfBirth_Is_Too_Old()
        {
            var user = CreateValidUser();
            user.Profile.DateOfBirth = DateTime.Now.AddYears(-121);

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains("A data de nascimento não pode ser superior a 120 anos.", result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_Role_Is_Empty()
        {
            var user = CreateValidUser();
            user.Account.Role = "";

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains("A função do usuário é obrigatória.", result.Message);
        }

        [Fact]
        public void Should_Have_Error_When_Role_Is_Invalid()
        {
            var user = CreateValidUser();
            user.Account.Role = "SuperUser";

            var result = Assert.Throws<ValidationException>(() => _userValidator.ValidateAndThrow(user));

            Assert.Contains("Função inválida. Use 'User', 'Admin' ou 'Moderator'.", result.Message);
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
