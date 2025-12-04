using System.Threading.Tasks;

namespace Application.Service.Interface
{
    public interface IEmailService
    {
        Task SendPasswordResetAsync(string toEmail, string resetUrl);
        Task SendTestEmailAsync(string toEmail);
        Task SendRecoveryCodeAsync(string toEmail, string code, DateTime expiresAt);
    }
}
