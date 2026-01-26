using Application.Domain.Model.Education.Dtos;

namespace Application.Domain.Model.Education
{
    public class EducationReportImport
    {
        public string? Id { get; set; }
        public List<string> FileNames { get; set; } = new();
        public List<string> AttachmentNames { get; set; } = new();
        public string Status { get; set; } = EducationImportStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public EducationReportImportResult? Result { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
