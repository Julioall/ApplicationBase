namespace Application.Domain.Model.Moodle.Dtos
{
    public class CourseUpsertResult
    {
        public bool Created { get; set; }
        public bool Updated { get; set; }
        public MoodleCourse? Entity { get; set; }
    }
}
