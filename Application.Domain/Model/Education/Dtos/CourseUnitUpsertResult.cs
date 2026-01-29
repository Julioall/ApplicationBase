namespace Application.Domain.Model.Education.Dtos
{
    public class CourseUnitUpsertResult
    {
        public bool Created { get; set; }
        public bool Updated { get; set; }
        public CourseUnit? Entity { get; set; }
    }
}
