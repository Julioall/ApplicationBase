namespace Application.Domain.Model.Education
{
    public class UcDocument
    {
        public string? Id { get; set; }
        public required int EadId { get; set; }
        public required string Fullname { get; set; }
        public required long StartDate { get; set; }
        public required long EndDate { get; set; }
        public string? ViewUrl { get; set; }
        public string? CourseImage { get; set; }
        public string? CourseCategory { get; set; }
        public string? SchoolNameDerived { get; set; }
        public string? ProgramNameDerived { get; set; }
        public string? PeriodTextDerived { get; set; }
        // Novos campos trazidos do Moodle
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
