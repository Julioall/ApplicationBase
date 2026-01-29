namespace Application.Domain.Model.Moodle
{
    public class MoodleCategory
    {
        public string? Id { get; set; }
        public int MoodleId { get; set; }
        public required string Name { get; set; }
        public int ParentId { get; set; }
        public int Depth { get; set; }
        public string? Path { get; set; }
        public long LastSyncedAt { get; set; }
    }
}
