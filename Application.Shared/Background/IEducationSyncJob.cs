namespace Application.Shared.Background
{
    public interface IEducationSyncJob
    {
        Task RunAsync(string triggeredByUserId, string triggeredByName, CancellationToken cancellationToken = default);
    }
}
