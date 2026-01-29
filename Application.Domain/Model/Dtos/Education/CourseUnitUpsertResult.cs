using Application.Domain.Model.Dtos.Education;
using Application.Domain.Model.Education;

namespace Application.Domain.Model.Dtos.Education
{
    public class CourseUnitUpsertResult
    {
        public bool Created { get; set; }
        public bool Updated { get; set; }
        public CourseUnit? Entity { get; set; }
    }
}




