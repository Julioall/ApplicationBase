namespace Application.Domain.Model.Education
{
    public class StudentUcMap
    {
        public string? Id { get; set; }
        public required string StudentId { get; set; }
        public required string CourseUnitId { get; set; }
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    }
}
