namespace Application.Domain.Model.Moodle
{
    /// <summary>
    /// Tracks the status of Moodle data synchronization.
    /// </summary>
    public class MoodleSyncStatus
    {
        public const string DocumentId = "moodle/sync-status";

        public string Id { get; set; } = DocumentId;
        public long? LastSyncAt { get; set; }
        public long? ExpiresAt { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
        public string? TriggeredByUserId { get; set; }
        public string? TriggeredByName { get; set; }
        public long? TriggeredAt { get; set; }
    }
}
