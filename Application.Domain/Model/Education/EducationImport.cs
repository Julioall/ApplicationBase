using Application.Domain.Model.Education.Dtos;

namespace Application.Domain.Model.Education
{
    public class EducationImport
    {
        public string? Id { get; set; }
        public required string FileName { get; set; }
        public string Status { get; set; } = EducationImportStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int Processed { get; set; }
        public int CreatedUcs { get; set; }
        public int UpdatedUcs { get; set; }
        public int Linked { get; set; }
        public string? ErrorMessage { get; set; }
        public List<CourseImportError> Errors { get; set; } = new();
    }

    public static class EducationImportStatus
    {
        public const string Pending = "Pending";
        public const string Running = "Running";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
    }
}
