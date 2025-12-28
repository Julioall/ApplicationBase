using Application.Domain.Interface;
using Application.Domain.Model.WhatsApp;
using Application.Infrastructure.Service;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;

namespace Application.Infrastructure.Repository
{
    public class WhatsAppInstanceRepository : IWhatsAppInstanceRepository
    {
        private readonly IServiceRavenDB _serviceRavenDb;

        public WhatsAppInstanceRepository(IServiceRavenDB serviceRavenDb)
        {
            _serviceRavenDb = serviceRavenDb;
        }

        public async Task<WhatsAppInstance?> GetByIdAsync(string id)
        {
            return await _serviceRavenDb.AsyncSession.LoadAsync<WhatsAppInstance>(id);
        }

        public async Task<IReadOnlyCollection<WhatsAppInstance>> GetAdminInstancesAsync(string? search = null)
        {
            var query = _serviceRavenDb.AsyncSession.Query<WhatsAppInstance>()
                .Where(instance => !instance.IsPrivateUserInstance);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                query = query.Where(instance => instance.DisplayName.ToLower().Contains(term) || instance.OwnerUserId.ToLower().Contains(term));
            }

            return await query.OrderByDescending(instance => instance.CreatedAt).ToListAsync();
        }

        public async Task<IReadOnlyCollection<WhatsAppInstance>> GetUserInstancesAsync(string ownerUserId)
        {
            return await _serviceRavenDb.AsyncSession.Query<WhatsAppInstance>()
                .Where(instance => instance.IsPrivateUserInstance && instance.OwnerUserId == ownerUserId)
                .OrderByDescending(instance => instance.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> CountUserInstancesAsync(string ownerUserId)
        {
            return await _serviceRavenDb.AsyncSession.Query<WhatsAppInstance>()
                .Customize(x => x.WaitForNonStaleResults())
                .Where(instance => instance.IsPrivateUserInstance && instance.OwnerUserId == ownerUserId && instance.IsActive)
                .CountAsync();
        }

        public async Task CreateAsync(WhatsAppInstance instance)
        {
            await _serviceRavenDb.AsyncSession.StoreAsync(instance);
            await _serviceRavenDb.AsyncSession.SaveChangesAsync();
        }

        public async Task UpdateAsync(WhatsAppInstance instance)
        {
            await _serviceRavenDb.AsyncSession.StoreAsync(instance);
            await _serviceRavenDb.AsyncSession.SaveChangesAsync();
        }

        public async Task DeleteAsync(string id)
        {
            _serviceRavenDb.AsyncSession.Delete(id);
            await _serviceRavenDb.AsyncSession.SaveChangesAsync();
        }
    }
}
