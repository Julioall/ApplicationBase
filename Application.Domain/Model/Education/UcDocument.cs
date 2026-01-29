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

        // Campos derivados da hierarquia de categorias do Moodle
        // Path exemplo: /84/87/6375/6406 → Instituição/Escola/Curso/Turma

        /// <summary>
        /// Nome da instituição (depth 1) - Ex: SENAI, SESI
        /// </summary>
        public string? InstitutionName { get; set; }

        /// <summary>
        /// ID da instituição no Moodle (depth 1)
        /// </summary>
        public int? InstitutionMoodleId { get; set; }

        /// <summary>
        /// Nome da escola (depth 2) - Ex: Escola SENAI Vila Canaã
        /// </summary>
        public string? SchoolName { get; set; }

        /// <summary>
        /// ID da escola no Moodle (depth 2)
        /// </summary>
        public int? SchoolMoodleId { get; set; }

        /// <summary>
        /// Nome do curso (depth 3) - Ex: Operador de Computador
        /// </summary>
        public string? CourseName { get; set; }

        /// <summary>
        /// ID do curso no Moodle (depth 3)
        /// </summary>
        public int? CourseMoodleId { get; set; }

        /// <summary>
        /// Nome da turma/evento (depth 4) - Ex: 1003121 - Operador de Computador - 00003/2025
        /// </summary>
        public string? EventName { get; set; }

        /// <summary>
        /// ID da turma/evento no Moodle (depth 4)
        /// </summary>
        public int? EventMoodleId { get; set; }

        // Campos legados - mantidos para compatibilidade
        [Obsolete("Use InstitutionName, SchoolName, CourseName, EventName instead")]
        public string? SchoolNameDerived { get; set; }

        [Obsolete("Use CourseName instead")]
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
