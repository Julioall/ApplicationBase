using Application.Domain.Interface;
using Application.Domain.Model.Notification;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using System.Linq;

namespace Application.Infrastructure.Repository
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly IServiceRavenDB _serviceRavenDb;

        public NotificationRepository(IServiceRavenDB serviceRavenDb)
        {
            _serviceRavenDb = serviceRavenDb;
        }

        public async Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(notification);

            await _serviceRavenDb.AsyncSession.StoreAsync(notification, notification.Id, cancellationToken);
            if (string.IsNullOrWhiteSpace(notification.Id))
            {
                notification.Id = _serviceRavenDb.AsyncSession.Advanced.GetDocumentId(notification);
            }

            return notification;
        }

        public async Task<IReadOnlyCollection<Notification>> GetLatestAsync(int take, CancellationToken cancellationToken = default)
        {
            var safeTake = Math.Clamp(take, 1, 100);
            var items = await _serviceRavenDb.AsyncSession.Query<Notification>()
                .OrderByDescending(x => x.CreatedAt)
                .Take(safeTake)
                .ToListAsync(cancellationToken);

            return items;
        }

        public Task<int> CountUnreadAsync(CancellationToken cancellationToken = default)
        {
            return _serviceRavenDb.AsyncSession.Query<Notification>()
                .CountAsync(x => !x.IsRead, cancellationToken);
        }

        public async Task<bool> MarkAsReadAsync(string id, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            var notification = await _serviceRavenDb.AsyncSession.LoadAsync<Notification>(id, cancellationToken);
            if (notification == null)
            {
                return false;
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await _serviceRavenDb.AsyncSession.StoreAsync(notification, id, cancellationToken);
            }

            return true;
        }

        public async Task<int> MarkAsReadAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
        {
            var idArray = ids?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray() ?? Array.Empty<string>();
            if (idArray.Length == 0)
            {
                return 0;
            }

            var loaded = await _serviceRavenDb.AsyncSession.LoadAsync<Notification>(idArray, cancellationToken);
            var changed = 0;
            foreach (var item in loaded.Values)
            {
                if (item != null && !item.IsRead)
                {
                    item.IsRead = true;
                    changed++;
                }
            }

            return changed;
        }

        public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            var notification = await _serviceRavenDb.AsyncSession.LoadAsync<Notification>(id, cancellationToken);
            if (notification == null)
            {
                return false;
            }

            _serviceRavenDb.AsyncSession.Delete(notification);
            return true;
        }
    }
}
