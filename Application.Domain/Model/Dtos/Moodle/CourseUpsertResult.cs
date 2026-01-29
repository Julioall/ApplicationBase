using Application.Domain.Model.Dtos.Moodle;
using Application.Domain.Model.Moodle;

namespace Application.Domain.Model.Dtos.Moodle
{
    public class CourseUpsertResult
    {
        public bool Created { get; set; }
        public bool Updated { get; set; }
        public MoodleCourse? Entity { get; set; }
    }
}




