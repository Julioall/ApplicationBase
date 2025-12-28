using Application.Domain.Model.WhatsApp;

namespace Application.Service.Interface
{
    public interface IWhatsAppInstanceProvider
    {
        Task ProvisionAsync(WhatsAppInstance instance, CancellationToken cancellationToken = default);
        Task<string> GetQrCodeAsync(WhatsAppInstance instance, CancellationToken cancellationToken = default);
        Task<string> GetStatusAsync(WhatsAppInstance instance, CancellationToken cancellationToken = default);
        Task DeactivateAsync(WhatsAppInstance instance, CancellationToken cancellationToken = default);
    }
}
