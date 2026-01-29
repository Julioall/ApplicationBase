using Application.Domain.Model.Dtos.Moodle;
using Application.Domain.Model.Moodle;

namespace Application.Domain.Model.Dtos.Moodle
{
    /// <summary>
    /// DTO that combines Student information with their performance in a specific Course.
    /// Used when returning student details within a course.
    /// </summary>
    public class StudentCourseDto
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? IdNumber { get; set; }
        public string? Phone { get; set; }
        public string? Institution { get; set; }
        public bool IsActive { get; set; }
        public string? Status { get; set; }
        public DateTime? LastAccessAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Course Performance
        public decimal? FinalGrade { get; set; }
        public List<StudentActivityDto> Activities { get; set; } = new();
    }

    /// <summary>
    /// DTO for student activities in a course.
    /// </summary>
    public class StudentActivityDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal? FinalGrade { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? CorrectedAt { get; set; }
        public string? SubmissionStatus { get; set; }
        public string? Restriction { get; set; }
        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }

        /// <summary>Activity type (ActivityType enum value)</summary>
        public int Type { get; set; } = (int)ActivityType.RequiresCorrection;

        /// <summary>Automatically calculated correction status</summary>
        public string CorrectionStatus { get; set; } = string.Empty;

        /// <summary>Indicates if correction is pending</summary>
        public bool IsPendingCorrection { get; set; }

        /// <summary>Indicates if there is a restriction</summary>
        public bool HasRestriction { get; set; }

        /// <summary>Indicates if submitted late</summary>
        public bool IsLate { get; set; }

        /// <summary>Indicates if the activity is hidden (by course configuration)</summary>
        public bool Hidden { get; set; }
    }
}




