namespace Application.Domain.Model.Moodle
{
    /// <summary>
    /// Represents a Moodle cohort (e.g., Class/Group of students).
    /// </summary>
    public class MoodleCohort
    {
        public string? Id { get; set; }
        public required string CategoryId { get; set; }
        public required string CourseCategoryId { get; set; }
        public required string Name { get; set; }
        public required string CourseCategoryRaw { get; set; }
        public long? StartDate { get; set; }
        public long? EndDate { get; set; }
    }
}
