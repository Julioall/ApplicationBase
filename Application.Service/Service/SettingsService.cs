using Application.Domain.Interface;
using Application.Domain.Model;
using Application.Service.Interface;
using System.Threading.Tasks;

namespace Application.Service.Service
{
    public class SettingsService : ISettingsService
    {
        private readonly ISettingsRepository _settingsRepository;

        public SettingsService(ISettingsRepository settingsRepository)
        {
            _settingsRepository = settingsRepository;
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
            return cfg.Email;
        }

        public async Task<EmailSettings> SaveEmailAsync(EmailSettings settings)
        {
            var cfg = await GetOrCreateAsync();
            cfg.Email = settings;
            await _settingsRepository.SaveAsync(cfg);
            return cfg.Email;
        }
    }
}
