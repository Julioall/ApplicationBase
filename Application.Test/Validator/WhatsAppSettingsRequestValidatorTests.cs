using Application.Domain.Model.Dtos;
using Application.Tests.Setup;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Tests.Validator
{
    public class WhatsAppSettingsRequestValidatorTests : BaseTest
    {
        private readonly IValidator<WhatsAppSettingsRequest> _validator;

        public WhatsAppSettingsRequestValidatorTests()
        {
            _validator = _serviceProvider.GetService<IValidator<WhatsAppSettingsRequest>>()
                ?? throw new Exception($"{nameof(IValidator<WhatsAppSettingsRequest>)} nao foi encontrado");
        }

        [Fact]
        public async Task Should_Fail_When_MaxUserInstances_Invalid()
        {
            var result = await _validator.ValidateAsync(new WhatsAppSettingsRequest
            {
                MaxUserInstances = 0
            });

            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task Should_Pass_When_MaxUserInstances_Valid()
        {
            var result = await _validator.ValidateAsync(new WhatsAppSettingsRequest
            {
                MaxUserInstances = 1
            });

            Assert.True(result.IsValid);
        }
    }
}
