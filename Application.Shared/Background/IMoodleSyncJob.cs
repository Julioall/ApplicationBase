namespace Application.Shared.Background
{
    public interface IMoodleSyncJob
    {
        Task RunAsync(string triggeredByUserId, string triggeredByName, CancellationToken cancellationToken = default);
    }
}
