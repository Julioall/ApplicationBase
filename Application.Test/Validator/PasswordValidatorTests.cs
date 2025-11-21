using Application.Domain;
using Application.Domain.Model.Dtos;
using Application.Domain.Validator;
using Microsoft.Extensions.Localization;

namespace Application.Tests.Validator
{
    public class PasswordValidatorTests
    {
        private readonly PasswordValidator _validator;

        private sealed class FakeLocalizer : IStringLocalizer<SharedResource>
        {
            public LocalizedString this[string name] => new(name, name);
            public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));
            public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
            public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
        }

        public PasswordValidatorTests()
        {
            _validator = new PasswordValidator(new FakeLocalizer());
        }

        [Theory]
        [InlineData("")]
        [InlineData("short")]
        [InlineData("nocaps123!")]
        [InlineData("NOLOWER123!")]
        [InlineData("NoNumber!")]
        [InlineData("NoSpecial123")]
        public void Should_Fail_For_Invalid_Passwords(string password)
        {
            var input = new PasswordInput { Password = password };
            var result = _validator.Validate(input);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Should_Pass_For_Strong_Password()
        {
            var input = new PasswordInput { Password = "Valid123!" };
            var result = _validator.Validate(input);

            Assert.True(result.IsValid);
        }
    }
}
