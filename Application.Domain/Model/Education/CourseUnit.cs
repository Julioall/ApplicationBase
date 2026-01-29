namespace Application.Domain.Model.Education
{
    /// <summary>
    /// Represents a Course Unit (UC) from Moodle.
    /// This is an individual course within the Moodle hierarchy.
    /// </summary>
    public class CourseUnit
    {
        public string? Id { get; set; }
        public required int EadId { get; set; }
        public required string Fullname { get; set; }
        public required long StartDate { get; set; }
        public required long EndDate { get; set; }
        public string? ViewUrl { get; set; }
        public string? CourseImage { get; set; }
        public string? CourseCategory { get; set; }

        // Moodle category hierarchy fields
        // Path example: /84/87/6375/6406 → Institution/School/Course/Event

        /// <summary>
        /// Institution name (depth 1) - Ex: SENAI, SESI
        /// </summary>
        public string? InstitutionName { get; set; }

        /// <summary>
        /// Institution ID in Moodle (depth 1)
        /// </summary>
        public int? InstitutionMoodleId { get; set; }

        /// <summary>
        /// School name (depth 2) - Ex: Escola SENAI Vila Canaã
        /// </summary>
        public string? SchoolName { get; set; }

        /// <summary>
        /// School ID in Moodle (depth 2)
        /// </summary>
        public int? SchoolMoodleId { get; set; }

        /// <summary>
        /// Course/Program name (depth 3) - Ex: Operador de Computador
        /// </summary>
        public string? CourseName { get; set; }

        /// <summary>
        /// Course ID in Moodle (depth 3)
        /// </summary>
        public int? CourseMoodleId { get; set; }

        /// <summary>
        /// Event/Class name (depth 4) - Ex: 1003121 - Operador de Computador - 00003/2025
        /// </summary>
        public string? EventName { get; set; }

        /// <summary>
        /// Event ID in Moodle (depth 4)
        /// </summary>
        public int? EventMoodleId { get; set; }

        public string? PeriodTextDerived { get; set; }

        // Additional fields from Moodle
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
