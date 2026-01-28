namespace Application.Domain.Model.Moodle
{
    /// <summary>
    /// Maps a Moodle cohort to its courses.
    /// </summary>
    public class MoodleCohortCourseMap
    {
        public string? Id { get; set; }
        public required string CohortId { get; set; }
        public required string CourseId { get; set; }
    }
}
