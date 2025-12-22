namespace Application.Domain.Model.Education.Dtos
{
    public class UcUpsertResult
    {
        public bool Created { get; set; }
        public bool Updated { get; set; }
        public UcDocument? Entity { get; set; }
    }
}
