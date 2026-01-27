using Application.Service.Service.Moodle;

namespace Application.Service.Interface
{
    public interface IMoodleAuthClient
    {
        Task<string?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);
        Task<MoodleSiteInfo?> GetSiteInfoAsync(string token, CancellationToken cancellationToken = default);
    }
}
