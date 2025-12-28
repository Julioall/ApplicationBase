using Application.Domain;
using Application.Domain.Exceptions;
using Application.Domain.Interface;
using Application.Domain.Model;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.WhatsApp;
using Application.Service.Interface;
using FluentValidation;
using Microsoft.Extensions.Localization;
using System.Linq;

namespace Application.Service.Service
{
    public class WhatsAppInstanceService : IWhatsAppInstanceService
    {
        private readonly IWhatsAppInstanceRepository _repository;
        private readonly ISettingsService _settingsService;
        private readonly IWhatsAppInstanceProvider _provider;
        private readonly IValidator<CreateWhatsAppInstanceRequest> _createValidator;
        private readonly IValidator<UpdateWhatsAppInstanceRequest> _updateValidator;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public WhatsAppInstanceService(
            IWhatsAppInstanceRepository repository,
            ISettingsService settingsService,
            IWhatsAppInstanceProvider provider,
            IValidator<CreateWhatsAppInstanceRequest> createValidator,
            IValidator<UpdateWhatsAppInstanceRequest> updateValidator,
            IStringLocalizer<SharedResource> localizer)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
            _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public Task<IReadOnlyCollection<WhatsAppInstance>> GetAdminInstancesAsync(string? search, CancellationToken cancellationToken = default)
        {
            return _repository.GetAdminInstancesAsync(search);
        }

        public Task<IReadOnlyCollection<WhatsAppInstance>> GetUserInstancesAsync(string ownerUserId, CancellationToken cancellationToken = default)
        {
            return _repository.GetUserInstancesAsync(ownerUserId);
        }

        public async Task<WhatsAppInstance> CreateAdminInstanceAsync(string ownerUserId, CreateWhatsAppInstanceRequest request, CancellationToken cancellationToken = default)
        {
            await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
            return await CreateInstanceAsync(ownerUserId, request.DisplayName, request.PhoneNumber, isPrivate: false, cancellationToken);
        }

        public async Task<WhatsAppInstance> CreateUserInstanceAsync(string ownerUserId, CreateWhatsAppInstanceRequest request, CancellationToken cancellationToken = default)
        {
            await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
            await EnforceUserQuotaAsync(ownerUserId);
            return await CreateInstanceAsync(ownerUserId, request.DisplayName, request.PhoneNumber, isPrivate: true, cancellationToken);
        }

        public async Task<WhatsAppInstance> RenameInstanceAsync(string ownerUserId, string id, UpdateWhatsAppInstanceRequest request, bool isAdminContext, CancellationToken cancellationToken = default)
        {
            await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
            var instance = await GetInstanceForContextAsync(ownerUserId, id, isAdminContext);

            instance.DisplayName = request.DisplayName.Trim();
            instance.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(instance);
            return instance;
        }

        public async Task<WhatsAppInstance> DisconnectInstanceAsync(string ownerUserId, string id, bool isAdminContext, CancellationToken cancellationToken = default)
        {
            var instance = await GetInstanceForContextAsync(ownerUserId, id, isAdminContext);
            EnsureActive(instance);

            await _provider.DisconnectAsync(instance, cancellationToken);
            instance.Status = WhatsAppInstanceStatus.Disconnected;
            instance.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(instance);
            return instance;
        }

        public async Task<WhatsAppInstance> DeleteInstanceAsync(string ownerUserId, string id, bool isAdminContext, CancellationToken cancellationToken = default)
        {
            var instance = await GetInstanceForContextAsync(ownerUserId, id, isAdminContext);

            await _provider.DeactivateAsync(instance, cancellationToken);
            await _repository.DeleteAsync(instance.Id);
            return instance;
        }

        public async Task<string> GetQrCodeAsync(string ownerUserId, string id, bool isAdminContext, CancellationToken cancellationToken = default)
        {
            var instance = await GetInstanceForContextAsync(ownerUserId, id, isAdminContext);
            EnsureActive(instance);

            var qr = await _provider.GetQrCodeAsync(instance, cancellationToken);
            return qr;
        }

        public async Task<WhatsAppInstance> RefreshStatusAsync(string ownerUserId, string id, bool isAdminContext, CancellationToken cancellationToken = default)
        {
            var instance = await GetInstanceForContextAsync(ownerUserId, id, isAdminContext);
            EnsureActive(instance);

            var status = await _provider.GetStatusAsync(instance, cancellationToken);
            instance.Status = status;
            instance.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(instance);

            return instance;
        }

        private async Task<WhatsAppInstance> CreateInstanceAsync(string ownerUserId, string displayName, string phoneNumber, bool isPrivate, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(ownerUserId))
            {
                throw new BusinessException(_localizer["WhatsAppOwnerRequired"]);
            }

            var instance = new WhatsAppInstance
            {
                DisplayName = displayName.Trim(),
                InternalInstanceName = GenerateInternalName(ownerUserId, isPrivate),
                OwnerUserId = ownerUserId,
                PhoneNumber = phoneNumber.Trim(),
                IsPrivateUserInstance = isPrivate,
                Status = WhatsAppInstanceStatus.Pending,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _provider.ProvisionAsync(instance, cancellationToken);
            await _repository.CreateAsync(instance);
            return instance;
        }

        private async Task EnforceUserQuotaAsync(string ownerUserId)
        {
            var settings = await _settingsService.GetWhatsAppAsync();
            var max = settings.MaxUserInstances <= 0 ? 1 : settings.MaxUserInstances;
            var count = await _repository.CountUserInstancesAsync(ownerUserId);
            if (count >= max)
            {
                throw new BusinessException(_localizer["WhatsAppUserQuotaExceeded", max]);
            }
        }

        private async Task<WhatsAppInstance> GetInstanceForContextAsync(string ownerUserId, string id, bool isAdminContext)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new BusinessException(_localizer["WhatsAppInstanceIdRequired"]);
            }

            var instance = await _repository.GetByIdAsync(id);
            if (instance == null)
            {
                throw new NotFoundException(_localizer["WhatsAppInstanceNotFound", id]);
            }

            if (isAdminContext)
            {
                if (instance.IsPrivateUserInstance)
                {
                    throw new ForbiddenException(_localizer["WhatsAppInstanceForbidden"]);
                }
                return instance;
            }

            if (!instance.IsPrivateUserInstance || !string.Equals(instance.OwnerUserId, ownerUserId, StringComparison.OrdinalIgnoreCase))
            {
                throw new ForbiddenException(_localizer["WhatsAppInstanceForbidden"]);
            }

            return instance;
        }

        private void EnsureActive(WhatsAppInstance instance)
        {
            if (!instance.IsActive)
            {
                throw new BusinessException(_localizer["WhatsAppInstanceInactive"]);
            }
        }

        private static string GenerateInternalName(string ownerUserId, bool isPrivate)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var prefix = isPrivate ? "user" : "admin";
            var normalizedOwner = NormalizeKey(ownerUserId);
            return $"wa-{prefix}-{normalizedOwner}-{suffix}";
        }

        private static string NormalizeKey(string value)
        {
            var buffer = value.Trim().ToLowerInvariant();
            var chars = buffer.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray();
            var normalized = new string(chars);
            while (normalized.Contains("--", StringComparison.Ordinal))
            {
                normalized = normalized.Replace("--", "-", StringComparison.Ordinal);
            }
            return normalized.Trim('-');
        }
    }
}
