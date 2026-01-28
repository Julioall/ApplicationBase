namespace Application.Domain.Model.Education
{
    public class EducationSyncStatus
    {
        public string Id { get; set; } = "education/sync-status";
        public long? LastSyncAt { get; set; }
        public long? ExpiresAt { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
        public string? TriggeredByUserId { get; set; }
        public string? TriggeredByName { get; set; }
        public long? TriggeredAt { get; set; }
    }
}
