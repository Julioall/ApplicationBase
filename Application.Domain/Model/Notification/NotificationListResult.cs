namespace Application.Domain.Model.Notification
{
    public class NotificationListResult
    {
        public IReadOnlyCollection<Notification> Items { get; set; } = Array.Empty<Notification>();
        public int UnreadCount { get; set; }
    }
}
