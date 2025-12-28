using Application.Domain.Model.Dtos;
using Application.Domain.Model.WhatsApp;

namespace Application.Service.Interface
{
    public interface IWhatsAppInstanceService
    {
        Task<IReadOnlyCollection<WhatsAppInstance>> GetAdminInstancesAsync(string? search, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<WhatsAppInstance>> GetUserInstancesAsync(string ownerUserId, CancellationToken cancellationToken = default);
        Task<WhatsAppInstance> CreateAdminInstanceAsync(string ownerUserId, CreateWhatsAppInstanceRequest request, CancellationToken cancellationToken = default);
        Task<WhatsAppInstance> CreateUserInstanceAsync(string ownerUserId, CreateWhatsAppInstanceRequest request, CancellationToken cancellationToken = default);
        Task<WhatsAppInstance> RenameInstanceAsync(string ownerUserId, string id, UpdateWhatsAppInstanceRequest request, bool isAdminContext, CancellationToken cancellationToken = default);
        Task<WhatsAppInstance> DeactivateInstanceAsync(string ownerUserId, string id, bool isAdminContext, CancellationToken cancellationToken = default);
        Task<string> GetQrCodeAsync(string ownerUserId, string id, bool isAdminContext, CancellationToken cancellationToken = default);
        Task<WhatsAppInstance> RefreshStatusAsync(string ownerUserId, string id, bool isAdminContext, CancellationToken cancellationToken = default);
    }
}
