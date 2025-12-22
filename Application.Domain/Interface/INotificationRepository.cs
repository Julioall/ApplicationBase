using Application.Domain.Model.Notification;

namespace Application.Domain.Interface
{
    public interface INotificationRepository
    {
        Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Notification>> GetLatestAsync(int take, CancellationToken cancellationToken = default);
        Task<int> CountUnreadAsync(CancellationToken cancellationToken = default);
        Task<bool> MarkAsReadAsync(string id, CancellationToken cancellationToken = default);
        Task<int> MarkAsReadAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}
