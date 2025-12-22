namespace Application.Domain.Model.Education
{
    public class ClassUcMap
    {
        public string? Id { get; set; }
        public required string ClassId { get; set; }
        public required string UcId { get; set; }
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    }
}
