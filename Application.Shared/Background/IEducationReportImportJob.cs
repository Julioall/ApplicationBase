namespace Application.Shared.Background
{
    public interface IEducationReportImportJob
    {
        Task ProcessReportAsync(string importId, CancellationToken cancellationToken = default);
    }
}
