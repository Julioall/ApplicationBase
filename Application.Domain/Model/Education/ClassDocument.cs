namespace Application.Domain.Model.Education
{
    public class ClassDocument
    {
        public string? Id { get; set; }
        public required string SchoolId { get; set; }
        public required string ProgramId { get; set; }
        public required string Name { get; set; }
        public required string CourseCategoryRaw { get; set; }
        public long? StartDate { get; set; }
        public long? EndDate { get; set; }
    }
}
