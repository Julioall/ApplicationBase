using System;

namespace Application.Api.RateLimiting
{
    public interface IRateLimiter
    {
        bool TryConsume(string key, int limit, TimeSpan window, out TimeSpan? retryAfter);
    }
}
