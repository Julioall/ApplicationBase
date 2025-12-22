using Application.Domain;
using Application.Domain.Exceptions;
using Application.Domain.Model;
using Application.Service.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Localization;

namespace Application.Service.Service
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _defaultSettings;
        private readonly ISettingsService _settingsService;
        private readonly ILogger<EmailService> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public EmailService(IOptions<EmailSettings> settings, ISettingsService settingsService, ILogger<EmailService> logger, IStringLocalizer<SharedResource> localizer)
        {
            _defaultSettings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
            _settingsService = settingsService;
            _logger = logger;
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task SendPasswordResetAsync(string toEmail)
        {
            var settings = await GetEffectiveSettingsAsync();

            ValidateEmailSettings(settings);
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new BusinessException(_localizer["EmailDestinationRequired"]);

            var message = BuildResetMessage(toEmail, settings);
            using var client = BuildClient(settings);
            await client.SendMailAsync(message);
        }

        public async Task SendRecoveryCodeAsync(string toEmail, string code, DateTime expiresAt)
        {
            var settings = await GetEffectiveSettingsAsync();

            ValidateEmailSettings(settings);
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new BusinessException(_localizer["EmailDestinationRequired"]);
            if (string.IsNullOrWhiteSpace(code))
                throw new BusinessException(_localizer["EmailCodeRequired"]);

            var from = new MailAddress(settings.FromEmail, settings.FromName);
            var message = new MailMessage
            {
                From = from,
                Subject = _localizer["EmailRecoverySubject"],
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
                if (stored != null && stored.Password.StartsWith("hash:", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ConfigurationException(_localizer["EmailPasswordStoredAsHash"]);
                }
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
                throw new ConfigurationException(_localizer["EmailSmtpHostInvalid"]);
            if (settings.Port <= 0)
                throw new ConfigurationException(_localizer["EmailSmtpPortInvalid"]);

            var client = new SmtpClient(settings.Host, settings.Port)
            {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                EnableSsl = ShouldUseSsl(settings.Secure),
            };

            if (string.IsNullOrWhiteSpace(settings.FromEmail))
                throw new ConfigurationException(_localizer["EmailFromNotConfigured"]);
            if (string.IsNullOrWhiteSpace(settings.Password))
                throw new ConfigurationException(_localizer["EmailPasswordNotConfigured"]);

            client.Credentials = new NetworkCredential(settings.FromEmail, settings.Password);

            _logger.LogInformation(
                "SMTP client configurado. Host={Host}, Port={Port}, EnableSsl={EnableSsl}, User={User}",
                settings.Host, settings.Port, client.EnableSsl, settings.FromEmail
            );

            return client;
        }

        private MailMessage BuildResetMessage(string toEmail, EmailSettings settings)
        {
            var from = new MailAddress(settings.FromEmail, settings.FromName);
            var message = new MailMessage
            {
                From = from,
                Subject = _localizer["EmailResetSubject"],
                Body = BuildResetBody(settings),
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };
            message.To.Add(new MailAddress(toEmail));
            return message;
        }

        private void ValidateEmailSettings(EmailSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Host))
                throw new ConfigurationException(_localizer["EmailHostNotConfigured"]);
            if (string.IsNullOrWhiteSpace(settings.FromEmail))
                throw new ConfigurationException(_localizer["EmailFromNotConfigured"]);
            if (string.IsNullOrWhiteSpace(settings.Password))
                throw new ConfigurationException(_localizer["EmailPasswordNotConfigured"]);
        }

        public async Task SendTestEmailAsync(string toEmail, EmailSettings? overrideSettings = null)
        {
            var settings = overrideSettings ?? await GetEffectiveSettingsAsync();

            ValidateEmailSettings(settings);
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new BusinessException(_localizer["EmailDestinationRequired"]);

            var from = new MailAddress(settings.FromEmail, settings.FromName);
            var message = new MailMessage
            {
                From = from,
                Subject = _localizer["EmailTestSubject"],
                Body = _localizer["EmailTestBody"],
                IsBodyHtml = false,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };
            message.To.Add(new MailAddress(toEmail));

            using var client = BuildClient(settings);
            try
            {
                await client.SendMailAsync(message);
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "SMTP send test failed: StatusCode={StatusCode}, Host={Host}, Port={Port}", smtpEx.StatusCode, client.Host, client.Port);
                throw new BusinessException(_localizer["EmailTestSendFailedWithCode", smtpEx.StatusCode]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Send test email failed");
                throw new BusinessException(_localizer["EmailTestSendFailed"]);
            }
        }

        private string BuildResetBody(EmailSettings settings)
        {
            return _localizer["EmailResetBody"];
        }

        private string BuildRecoveryBody(string code, DateTime expiresAt)
        {
            var expiry = expiresAt.ToLocalTime().ToString("g");
            return _localizer["EmailRecoveryBody", WebUtility.HtmlEncode(code), WebUtility.HtmlEncode(expiry)];
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
