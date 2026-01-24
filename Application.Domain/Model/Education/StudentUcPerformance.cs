namespace Application.Domain.Model.Education
{
    /// <summary>Tipos de atividades para determinar se requerem correção</summary>
    public enum ActivityType
    {
        /// <summary>Atividade que requer correção (exercícios, trabalhos, etc.)</summary>
        RequiresCorrection = 0,
        
        /// <summary>Questionários que não requerem correção manual (auto-avaliação, testes automáticos)</summary>
        AutoGraded = 1,
        
        /// <summary>Atividades de discussão ou participação</summary>
        Participation = 2,
        
        /// <summary>Atividade não categorizada</summary>
        Unknown = 99
    }

    /// <summary>Configuração de atividades ocultas para uma UC (global para todos os alunos)</summary>
    public class HiddenActivitiesConfig
    {
        public string? Id { get; set; }
        public string UcId { get; set; } = string.Empty;
        public List<string> HiddenActivityNames { get; set; } = new();
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public void ToggleActivityName(string activityName)
        {
            ArgumentNullException.ThrowIfNull(activityName);
            
            var index = HiddenActivityNames.FindIndex(a => 
                a.Equals(activityName, StringComparison.OrdinalIgnoreCase));
            
            if (index >= 0)
            {
                HiddenActivityNames.RemoveAt(index);
            }
            else
            {
                HiddenActivityNames.Add(activityName);
            }
            
            UpdatedAt = DateTime.UtcNow;
        }

        public bool IsHidden(string activityName)
        {
            return HiddenActivityNames.Any(a => 
                a.Equals(activityName, StringComparison.OrdinalIgnoreCase));
        }
    }

    public class StudentUcPerformance
    {
        public string? Id { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string UcId { get; set; } = string.Empty;
        public DateTime? LastAccessAt { get; set; }
        public decimal? FinalGrade { get; set; }
        public List<StudentActivity> Activities { get; set; } = new();
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public void Merge(StudentUcPerformance other)
        {
            if (other.LastAccessAt.HasValue)
            {
                LastAccessAt = LastAccessAt.HasValue
                    ? new[] { LastAccessAt.Value, other.LastAccessAt.Value }.Max()
                    : other.LastAccessAt;
            }

            if (other.FinalGrade.HasValue)
            {
                FinalGrade = other.FinalGrade;
            }

            MergeActivities(other.Activities);
            UpdatedAt = DateTime.UtcNow;
        }

        private void MergeActivities(List<StudentActivity> newActivities)
        {
            foreach (var newActivity in newActivities)
            {
                var existingActivity = Activities.FirstOrDefault(a =>
                    NormalizeKey(a.Name) == NormalizeKey(newActivity.Name));

                if (existingActivity != null)
                {
                    // Manter o registro mais recente
                    var existingDate = existingActivity.CorrectedAt ?? existingActivity.SubmittedAt;
                    var newDate = newActivity.CorrectedAt ?? newActivity.SubmittedAt;

                    if (newDate > existingDate)
                    {
                        Activities.Remove(existingActivity);
                        Activities.Add(newActivity);
                    }
                }
                else
                {
                    Activities.Add(newActivity);
                }
            }
        }

        private static string NormalizeKey(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
            return System.Text.RegularExpressions.Regex.Replace(text.Trim().ToLowerInvariant(), @"\s+", " ");
        }
    }

    public class StudentActivity
    {
        public string Name { get; set; } = string.Empty;
        public decimal? FinalGrade { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? CorrectedAt { get; set; }
        public string? SubmissionStatus { get; set; }
        public string? Restriction { get; set; }
        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }
        
        /// <summary>Tipo de atividade (padrão: RequiresCorrection)</summary>
        public ActivityType Type { get; set; } = ActivityType.RequiresCorrection;

        /// <summary>
        /// Determina o status de correção da atividade
        /// </summary>
        /// <returns>
        /// "Pendente de Correção" - Atividade enviada mas não corrigida
        /// "Corrigida" - Atividade foi corrigida
        /// "Não Enviada" - Atividade não foi enviada
        /// "Sem Correção Necessária" - Questionário/auto-avaliação (não requer correção)
        /// </returns>
        public string GetCorrectionStatus()
        {
            // Verificar se foi enviada
            if (!SubmittedAt.HasValue)
                return "Não Enviada";

            // Atividades que não requerem correção
            if (Type == ActivityType.AutoGraded || Type == ActivityType.Participation)
                return "Sem Correção Necessária";

            // Atividades que requerem correção
            if (Type == ActivityType.RequiresCorrection)
            {
                return CorrectedAt.HasValue ? "Corrigida" : "Pendente de Correção";
            }

            return "Desconhecido";
        }

        /// <summary>
        /// Verifica se a atividade está pendente de correção
        /// </summary>
        public bool IsPendingCorrection()
        {
            return SubmittedAt.HasValue 
                && !CorrectedAt.HasValue 
                && Type == ActivityType.RequiresCorrection;
        }

        /// <summary>
        /// Verifica se a atividade está com restrição (não pode ser corrigida ainda)
        /// </summary>
        public bool HasRestriction()
        {
            return !string.IsNullOrWhiteSpace(Restriction);
        }

        /// <summary>
        /// Verifica se a atividade está atrasada
        /// </summary>
        public bool IsLate()
        {
            if (!SubmittedAt.HasValue || !EndAt.HasValue)
                return false;

            return SubmittedAt.Value > EndAt.Value;
        }
    }
}
