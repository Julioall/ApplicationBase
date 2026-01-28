using Microsoft.Extensions.Caching.Memory;
using System;

namespace Application.Shared.Session
{
    public interface ISessionTokenCache
    {
        void SetMoodleToken(int moodleUserId, string token, TimeSpan ttl);
        string? GetMoodleToken(int moodleUserId);
    }

    public class SessionTokenCache : ISessionTokenCache
    {
        private readonly IMemoryCache _cache;
        public SessionTokenCache(IMemoryCache cache)
        {
            _cache = cache;
        }

        public void SetMoodleToken(int moodleUserId, string token, TimeSpan ttl)
        {
            _cache.Set($"moodle_token:{moodleUserId}", token, ttl);
        }

        public string? GetMoodleToken(int moodleUserId)
        {
            return _cache.TryGetValue($"moodle_token:{moodleUserId}", out string? token) ? token : null;
        }
    }
}
