using Application.Domain.Model.WhatsApp;

namespace Application.Domain.Interface
{
    public interface IWhatsAppInstanceRepository
    {
        Task<WhatsAppInstance?> GetByIdAsync(string id);
        Task<IReadOnlyCollection<WhatsAppInstance>> GetAdminInstancesAsync(string? search = null);
        Task<IReadOnlyCollection<WhatsAppInstance>> GetUserInstancesAsync(string ownerUserId);
        Task<int> CountUserInstancesAsync(string ownerUserId);
        Task CreateAsync(WhatsAppInstance instance);
        Task UpdateAsync(WhatsAppInstance instance);
    }
}
