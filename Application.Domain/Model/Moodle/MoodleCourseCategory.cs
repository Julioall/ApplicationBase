namespace Application.Domain.Model.Moodle
{
    /// <summary>
    /// Represents a Moodle course category (e.g., Program).
    /// </summary>
    public class MoodleCourseCategory
    {
        public string? Id { get; set; }
        public required string CategoryId { get; set; }
        public required string Name { get; set; }
    }
}
