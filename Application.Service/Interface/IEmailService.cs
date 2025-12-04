using System.Threading.Tasks;
using Application.Domain.Model;

namespace Application.Service.Interface
{
    public interface IEmailService
    {
        Task SendPasswordResetAsync(string toEmail);
        Task SendTestEmailAsync(string toEmail, EmailSettings? overrideSettings = null);
        Task SendRecoveryCodeAsync(string toEmail, string code, DateTime expiresAt);
    }
}
