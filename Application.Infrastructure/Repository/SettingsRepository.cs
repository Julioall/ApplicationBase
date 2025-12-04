using Application.Domain.Interface;
using Application.Domain.Model;
using System.Linq;
using System.Threading.Tasks;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents;

namespace Application.Infrastructure.Repository
{
    public class SettingsRepository : ISettingsRepository
    {
        private readonly IServiceRavenDB _serviceRavenDb;

        public SettingsRepository(IServiceRavenDB serviceRavenDb)
        {
            _serviceRavenDb = serviceRavenDb;
        }

        public async Task<Configurations?> GetAsync()
        {
            var list =  await _serviceRavenDb.AsyncSession.Query<Configurations>().ToListAsync();
            return list.FirstOrDefault();
        }

        public async Task SaveAsync(Configurations configurations)
        {
            await _serviceRavenDb.AsyncSession.StoreAsync(configurations);
            await _serviceRavenDb.AsyncSession.SaveChangesAsync();
        }
    }
}
