using Application.Domain;
using Application.Domain.Exceptions;
using Application.Domain.Interface;
using Application.Domain.Model;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.WhatsApp;
using Application.Service.Interface;
using Application.Service.Service;
using Application.Tests.Setup;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Application.Tests.Services
{
    public class WhatsAppInstanceServiceTests : BaseTest
    {
        private readonly IWhatsAppInstanceRepository _repository;
        private readonly ISettingsService _settingsService;
        private readonly IValidator<CreateWhatsAppInstanceRequest> _createValidator;
        private readonly IValidator<UpdateWhatsAppInstanceRequest> _updateValidator;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public WhatsAppInstanceServiceTests()
        {
            _repository = _serviceProvider.GetService<IWhatsAppInstanceRepository>()
                ?? throw new Exception($"{nameof(IWhatsAppInstanceRepository)} nao foi encontrado");
            _settingsService = _serviceProvider.GetService<ISettingsService>()
                ?? throw new Exception($"{nameof(ISettingsService)} nao foi encontrado");
            _createValidator = _serviceProvider.GetService<IValidator<CreateWhatsAppInstanceRequest>>()
                ?? throw new Exception($"{nameof(IValidator<CreateWhatsAppInstanceRequest>)} nao foi encontrado");
            _updateValidator = _serviceProvider.GetService<IValidator<UpdateWhatsAppInstanceRequest>>()
                ?? throw new Exception($"{nameof(IValidator<UpdateWhatsAppInstanceRequest>)} nao foi encontrado");
            _localizer = _serviceProvider.GetService<IStringLocalizer<SharedResource>>()
                ?? throw new Exception("Localizador nao encontrado");
        }

        [Fact]
        public async Task CreateUserInstance_Should_Enforce_MaxUserInstances()
        {
            await _settingsService.SaveWhatsAppAsync(new WhatsAppSettings { MaxUserInstances = 1 });

            var service = new WhatsAppInstanceService(
                _repository,
                _settingsService,
                new StubProvider(),
                _createValidator,
                _updateValidator,
                _localizer);

            await service.CreateUserInstanceAsync("users/1-A", new CreateWhatsAppInstanceRequest { DisplayName = "Main", PhoneNumber = "+5511999999999" });

            await Assert.ThrowsAsync<BusinessException>(() =>
                service.CreateUserInstanceAsync("users/1-A", new CreateWhatsAppInstanceRequest { DisplayName = "Second", PhoneNumber = "+5511999999999" }));
        }

        private sealed class StubProvider : IWhatsAppInstanceProvider
        {
            public Task ProvisionAsync(WhatsAppInstance instance, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task<string> GetQrCodeAsync(WhatsAppInstance instance, CancellationToken cancellationToken = default)
            {
                return Task.FromResult("qr");
            }

            public Task<string> GetStatusAsync(WhatsAppInstance instance, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(WhatsAppInstanceStatus.Pending);
            }

            public Task DeactivateAsync(WhatsAppInstance instance, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}
