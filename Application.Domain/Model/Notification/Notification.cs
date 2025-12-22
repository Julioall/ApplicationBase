namespace Application.Domain.Model.Notification
{
    public class Notification
    {
        public string? Id { get; set; }
        public required string Title { get; set; }
        public required string Message { get; set; }
        public string Type { get; set; } = NotificationType.Info;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ReferenceId { get; set; }
        public string? ReferenceType { get; set; }
        public string? Link { get; set; }
    }

    public static class NotificationType
    {
        public const string Success = "success";
        public const string Error = "error";
        public const string Warning = "warning";
        public const string Info = "info";
    }
}
