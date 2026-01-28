using System.Text.RegularExpressions;

namespace Application.Domain.Model.Moodle
{
    /// <summary>Types of activities to determine if correction is required</summary>
    public enum ActivityType
    {
        /// <summary>Activity that requires correction (exercises, assignments, etc.)</summary>
        RequiresCorrection = 0,

        /// <summary>Quizzes that don't require manual correction (self-assessment, auto-tests)</summary>
        AutoGraded = 1,

        /// <summary>Discussion or participation activities</summary>
        Participation = 2,

        /// <summary>Uncategorized activity</summary>
        Unknown = 99
    }

    /// <summary>Configuration for hidden activities in a course (global for all students)</summary>
    public class HiddenActivitiesConfig
    {
        public string? Id { get; set; }
        public string CourseId { get; set; } = string.Empty;
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

    /// <summary>
    /// Tracks a student's performance in a Moodle course.
    /// </summary>
    public class StudentCoursePerformance
    {
        public string? Id { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public DateTime? LastAccessAt { get; set; }
        public decimal? FinalGrade { get; set; }
        public List<StudentActivity> Activities { get; set; } = new();
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public void Merge(StudentCoursePerformance other)
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
            return Regex.Replace(text.Trim().ToLowerInvariant(), @"\s+", " ");
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

        /// <summary>Activity type (default: RequiresCorrection)</summary>
        public ActivityType Type { get; set; } = ActivityType.RequiresCorrection;

        /// <summary>
        /// Determines the correction status of the activity
        /// </summary>
        public string GetCorrectionStatus()
        {
            if (!SubmittedAt.HasValue)
                return "Não Enviada";

            if (Type == ActivityType.AutoGraded || Type == ActivityType.Participation)
                return "Sem Correção Necessária";

            if (Type == ActivityType.RequiresCorrection)
            {
                return CorrectedAt.HasValue ? "Corrigida" : "Pendente de Correção";
            }

            return "Desconhecido";
        }

        /// <summary>
        /// Checks if the activity is pending correction
        /// </summary>
        public bool IsPendingCorrection()
        {
            return SubmittedAt.HasValue
                && !CorrectedAt.HasValue
                && Type == ActivityType.RequiresCorrection;
        }

        /// <summary>
        /// Checks if the activity has a restriction
        /// </summary>
        public bool HasRestriction()
        {
            return !string.IsNullOrWhiteSpace(Restriction);
        }

        /// <summary>
        /// Checks if the activity was submitted late
        /// </summary>
        public bool IsLate()
        {
            if (!SubmittedAt.HasValue || !EndAt.HasValue)
                return false;

            return SubmittedAt.Value > EndAt.Value;
        }
    }
}
