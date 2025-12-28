using Application.Domain.Model;
using System.Threading.Tasks;

namespace Application.Service.Interface
{
    public interface ISettingsService
    {
        Task<Configurations> GetOrCreateAsync();
        Task<EmailSettings> GetEmailAsync();
        Task<EmailSettings> SaveEmailAsync(EmailSettings settings);
        Task<WhatsAppSettings> GetWhatsAppAsync();
        Task<WhatsAppSettings> SaveWhatsAppAsync(WhatsAppSettings settings);
    }
}
