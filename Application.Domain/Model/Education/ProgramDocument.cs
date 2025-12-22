namespace Application.Domain.Model.Education
{
    public class ProgramDocument
    {
        public string? Id { get; set; }
        public required string SchoolId { get; set; }
        public required string Name { get; set; }
    }
}
