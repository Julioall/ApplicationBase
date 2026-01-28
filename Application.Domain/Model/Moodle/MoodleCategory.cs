namespace Application.Domain.Model.Moodle
{
    /// <summary>
    /// Represents a top-level Moodle category (e.g., School/Unit).
    /// </summary>
    public class MoodleCategory
    {
        public string? Id { get; set; }
        public required string Name { get; set; }
    }
}
