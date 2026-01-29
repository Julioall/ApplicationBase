using Application.Domain.Model.Dtos.Education;
using Application.Domain.Model.Education;

namespace Application.Domain.Model.Dtos.Education
{
    /// <summary>
    /// DTO que combina informações do Student com seu Performance em uma UC específica
    /// Usado ao retornar detalhes de estudantes em uma unidade curricular
    /// </summary>
    public class StudentUcDto
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

        // Performance na UC
        public decimal? FinalGrade { get; set; }
        public List<StudentActivityDto> Activities { get; set; } = new();
    }

    /// <summary>
    /// DTO para atividades do estudante em uma UC
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
        
        /// <summary>Tipo de atividade (ActivityType enum value)</summary>
        public int Type { get; set; } = (int)ActivityType.RequiresCorrection;

        /// <summary>Status de correção da atividade calculado automaticamente</summary>
        public string CorrectionStatus { get; set; } = string.Empty;

        /// <summary>Indica se está pendente de correção</summary>
        public bool IsPendingCorrection { get; set; }

        /// <summary>Indica se está com restrição</summary>
        public bool HasRestriction { get; set; }

        /// <summary>Indica se foi enviada atrasada</summary>
        public bool IsLate { get; set; }

        /// <summary>Indica se a atividade está oculta (por configuração da UC)</summary>
        public bool Hidden { get; set; }
    }
}




