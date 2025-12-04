using Application.Domain.Model;
using Application.Service.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Application.Service.Service
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _defaultSettings;
        private readonly ISettingsService _settingsService;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ISettingsService settingsService, ILogger<EmailService> logger)
        {
            _defaultSettings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
            _settingsService = settingsService;
            _logger = logger;
        }

        public async Task SendPasswordResetAsync(string toEmail, string resetUrl)
        {
            var settings = await GetEffectiveSettingsAsync();

            if (string.IsNullOrWhiteSpace(settings.Host))
                throw new InvalidOperationException("Email host is not configured.");
            if (string.IsNullOrWhiteSpace(settings.FromEmail))
                throw new InvalidOperationException("From email is not configured.");
            if (string.IsNullOrWhiteSpace(settings.Password))
                throw new InvalidOperationException("SMTP password is not configured.");
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException("Destination email is required.", nameof(toEmail));

            var message = BuildResetMessage(toEmail, resetUrl, settings);
            using var client = BuildClient(settings);
            await client.SendMailAsync(message);
        }

        public async Task SendRecoveryCodeAsync(string toEmail, string code, DateTime expiresAt)
        {
            var settings = await GetEffectiveSettingsAsync();

            if (string.IsNullOrWhiteSpace(settings.Host))
                throw new InvalidOperationException("Email host is not configured.");
            if (string.IsNullOrWhiteSpace(settings.FromEmail))
                throw new InvalidOperationException("From email is not configured.");
            if (string.IsNullOrWhiteSpace(settings.Password))
                throw new InvalidOperationException("SMTP password is not configured.");
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException("Destination email is required.", nameof(toEmail));
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Code is required.", nameof(code));

            var from = new MailAddress(settings.FromEmail, settings.FromName);
            var message = new MailMessage
            {
                From = from,
                Subject = "Código de recuperação",
                Body = BuildRecoveryBody(code, expiresAt),
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };
            message.To.Add(new MailAddress(toEmail));

            using var client = BuildClient(settings);
            await client.SendMailAsync(message);
        }

        private async Task<EmailSettings> GetEffectiveSettingsAsync()
        {
            try
            {
                var stored = await _settingsService.GetEmailAsync();
                return stored ?? _defaultSettings;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falling back to default email settings");
                return _defaultSettings;
            }
        }

        private SmtpClient BuildClient(EmailSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Host))
                throw new InvalidOperationException("SMTP host não configurado.");
            if (settings.Port <= 0)
                throw new InvalidOperationException("SMTP port inválida.");

            var client = new SmtpClient(settings.Host, settings.Port)
            {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                EnableSsl = ShouldUseSsl(settings.Secure),
            };

            if (string.IsNullOrWhiteSpace(settings.FromEmail))
                throw new InvalidOperationException("FromEmail não configurado para autenticação SMTP.");
            if (string.IsNullOrWhiteSpace(settings.Password))
                throw new InvalidOperationException("Senha SMTP não configurada.");

            client.Credentials = new NetworkCredential(settings.FromEmail, settings.Password);

            _logger.LogInformation(
                "SMTP client configurado. Host={Host}, Port={Port}, EnableSsl={EnableSsl}, User={User}",
                settings.Host, settings.Port, client.EnableSsl, settings.FromEmail
            );

            return client;
        }

        private MailMessage BuildResetMessage(string toEmail, string resetUrl, EmailSettings settings)
        {
            var from = new MailAddress(settings.FromEmail, settings.FromName);
            var message = new MailMessage
            {
                From = from,
                Subject = "Recuperação de senha",
                Body = BuildResetBody(resetUrl, settings),
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };
            message.To.Add(new MailAddress(toEmail));
            return message;
        }

        public async Task SendTestEmailAsync(string toEmail)
        {
            var settings = await GetEffectiveSettingsAsync();

            if (string.IsNullOrWhiteSpace(settings.Host))
                throw new InvalidOperationException("Email host is not configured.");
            if (string.IsNullOrWhiteSpace(settings.FromEmail))
                throw new InvalidOperationException("From email is not configured.");
            if (string.IsNullOrWhiteSpace(settings.Password))
                throw new InvalidOperationException("SMTP password is not configured.");
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException("Destination email is required.", nameof(toEmail));

            var from = new MailAddress(settings.FromEmail, settings.FromName);
            var message = new MailMessage
            {
                From = from,
                Subject = "Teste de e-mail",
                Body = "Teste de envio de e-mail das configurações de serviço.",
                IsBodyHtml = false,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };
            message.To.Add(new MailAddress(toEmail));

            using var client = BuildClient(settings);
            await client.SendMailAsync(message);
        }

        private string BuildResetBody(string resetUrl, EmailSettings settings)
        {
            var link = string.IsNullOrWhiteSpace(resetUrl) ? settings.DefaultResetUrl : resetUrl;
            return $@"
                <p>Recebemos um pedido para redefinir a senha da sua conta.</p>
                <p>Clique no link abaixo para continuar:</p>
                <p><a href=""{WebUtility.HtmlEncode(link)}"">{WebUtility.HtmlEncode(link)}</a></p>
                <p>Se você não solicitou, ignore este e-mail.</p>";
        }

        private string BuildRecoveryBody(string code, DateTime expiresAt)
        {
            var expiry = expiresAt.ToLocalTime().ToString("g");
            return $@"
                <p>Seu código de recuperação é:</p>
                <p style=""font-size:20px;font-weight:bold;letter-spacing:2px;"">{WebUtility.HtmlEncode(code)}</p>
                <p>Ele expira em {WebUtility.HtmlEncode(expiry)}.</p>
                <p>Se você não solicitou, ignore este e-mail.</p>";
        }

        private bool ShouldUseSsl(string secure)
        {
            if (string.IsNullOrWhiteSpace(secure)) return false;
            return secure.Equals("ssl", StringComparison.OrdinalIgnoreCase) ||
                   secure.Equals("starttls", StringComparison.OrdinalIgnoreCase) ||
                   secure.Equals("tls", StringComparison.OrdinalIgnoreCase);
        }
    }
}
