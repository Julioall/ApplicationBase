namespace Application.Domain.Model.Moodle
{
    /// <summary>
    /// Maps a student to a Moodle course enrollment.
    /// </summary>
    public class StudentCourseMap
    {
        public string? Id { get; set; }
        public required string StudentId { get; set; }
        public required string CourseId { get; set; }
    }
}
