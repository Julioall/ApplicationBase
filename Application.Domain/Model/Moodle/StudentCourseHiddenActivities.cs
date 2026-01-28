namespace Application.Domain.Model.Moodle
{
    /// <summary>
    /// Tracks hidden activities for a student in a specific course.
    /// </summary>
    public class StudentCourseHiddenActivities
    {
        public string? Id { get; set; }
        public required string StudentId { get; set; }
        public required string CourseId { get; set; }
        public HashSet<string> HiddenActivityNames { get; set; } = new();

        public bool IsActivityHidden(string activityName) => HiddenActivityNames.Contains(activityName);

        public void ToggleActivityName(string activityName)
        {
            if (!HiddenActivityNames.Add(activityName))
            {
                HiddenActivityNames.Remove(activityName);
            }
        }
    }
}
