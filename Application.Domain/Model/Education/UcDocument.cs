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
    }
}
