using Application.Domain.Interface;
using Application.Domain.Model;
using Application.Service.Interface;

namespace Application.Service.Service
{
    public class SettingsService : ISettingsService
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly ISecretEncryptionService _secretEncryptionService;

        public SettingsService(ISettingsRepository settingsRepository, ISecretEncryptionService secretEncryptionService)
        {
            _settingsRepository = settingsRepository;
            _secretEncryptionService = secretEncryptionService;
        }

        public async Task<Configurations> GetOrCreateAsync()
        {
            var cfg = await _settingsRepository.GetAsync();
            if (cfg != null && cfg.Email != null)
            {
                return cfg;
            }

            var created = cfg ?? new Configurations();
            created.Email ??= new EmailSettings();
            await _settingsRepository.SaveAsync(created);
            return created;
        }

        public async Task<EmailSettings> GetEmailAsync()
        {
            var cfg = await GetOrCreateAsync();
            var email = cfg.Email ?? new EmailSettings();
            if (!string.IsNullOrWhiteSpace(email.Password))
            {
                try
                {
                    email = new EmailSettings
                    {
                        FromName = email.FromName,
                        FromEmail = email.FromEmail,
                        Host = email.Host,
                        Port = email.Port,
                        Secure = email.Secure,
                        Password = _secretEncryptionService.Decrypt(email.Password)
                    };
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Failed to decrypt email password. Reconfigure SMTP credentials.", ex);
                }
            }
            return email;
        }

        public async Task<EmailSettings> SaveEmailAsync(EmailSettings settings)
        {
            var cfg = await GetOrCreateAsync();
            var email = settings ?? new EmailSettings();
            if (!string.IsNullOrWhiteSpace(email.Password))
            {
                email = new EmailSettings
                {
                    FromName = email.FromName,
                    FromEmail = email.FromEmail,
                    Host = email.Host,
                    Port = email.Port,
                    Secure = email.Secure,
                    Password = _secretEncryptionService.Encrypt(email.Password)
                };
            }

            cfg.Email = email;
            await _settingsRepository.SaveAsync(cfg);

            // return plain text back to caller (do not re-encrypt)
            return settings ?? new EmailSettings();
        }
    }
}
