using Application.Domain.Interface;
using Application.Domain.Model.Notification;
using Application.Service.Interface;
using Microsoft.Extensions.Localization;

namespace Application.Service.Service
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IStringLocalizer<Application.Domain.SharedResource> _localizer;

        public NotificationService(INotificationRepository notificationRepository, IStringLocalizer<Application.Domain.SharedResource> localizer)
        {
            _notificationRepository = notificationRepository;
            _localizer = localizer;
        }

        public async Task<Notification> CreateAsync(string title, string message, string type, string? referenceId = null, string? referenceType = null, string? link = null, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentException.ThrowIfNullOrWhiteSpace(message);
            ArgumentException.ThrowIfNullOrWhiteSpace(type);

            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = type,
                ReferenceId = referenceId,
                ReferenceType = referenceType,
                Link = link,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            return await _notificationRepository.AddAsync(notification, cancellationToken);
        }

        public async Task<NotificationListResult> GetLatestAsync(int take, CancellationToken cancellationToken = default)
        {
            var items = await _notificationRepository.GetLatestAsync(take, cancellationToken);
            var unreadCount = await _notificationRepository.CountUnreadAsync(cancellationToken);

            return new NotificationListResult
            {
                Items = items,
                UnreadCount = unreadCount
            };
        }

        public async Task MarkAsReadAsync(string id, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            var changed = await _notificationRepository.MarkAsReadAsync(id, cancellationToken);
            if (!changed)
            {
                throw new Application.Domain.Exceptions.NotFoundException(_localizer["NotificationNotFound", id]);
            }
        }

        public async Task MarkManyAsReadAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            var changed = await _notificationRepository.MarkAsReadAsync(ids, cancellationToken);
            _ = changed;
        }

        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            var deleted = await _notificationRepository.DeleteAsync(id, cancellationToken);
            if (!deleted)
            {
                throw new Application.Domain.Exceptions.NotFoundException(_localizer["NotificationNotFound", id]);
            }
        }
    }
}
