namespace Application.Domain.Model.Moodle
{
    /// <summary>
    /// Represents a Moodle course (formerly CourseUnit/Curricular Unit).
    /// </summary>
    public class MoodleCourse
    {
        public string? Id { get; set; }
        public required int MoodleId { get; set; }
        public required string Fullname { get; set; }
        public required long StartDate { get; set; }
        public required long EndDate { get; set; }
        public string? ViewUrl { get; set; }
        public string? CourseImage { get; set; }
        public string? CourseCategory { get; set; }
        public string? CategoryNameDerived { get; set; }
        public string? CourseCategoryNameDerived { get; set; }
        public string? PeriodTextDerived { get; set; }
        public float? Progress { get; set; }
        public bool? Completed { get; set; }
        public bool? IsFavourite { get; set; }
        public bool? Hidden { get; set; }
        public string? Summary { get; set; }
        public int? LastAccess { get; set; }
        public string? IdNumber { get; set; }
        public string? Lang { get; set; }
    }
}
