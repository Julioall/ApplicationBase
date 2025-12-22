using Application.Domain.Model.Notification;

namespace Application.Service.Interface
{
    public interface INotificationService
    {
        Task<Notification> CreateAsync(string title, string message, string type, string? referenceId = null, string? referenceType = null, string? link = null, CancellationToken cancellationToken = default);
        Task<NotificationListResult> GetLatestAsync(int take, CancellationToken cancellationToken = default);
        Task MarkAsReadAsync(string id, CancellationToken cancellationToken = default);
        Task MarkManyAsReadAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}
