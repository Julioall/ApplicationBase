using System;
using Microsoft.Extensions.Caching.Memory;

namespace Application.Api.RateLimiting
{
    /// <summary>
    /// Simple in-memory counter-based rate limiter keyed by arbitrary strings.
    /// </summary>
    public class MemoryRateLimiter : IRateLimiter
    {
        private readonly IMemoryCache _cache;
        private static readonly object _lock = new();

        public MemoryRateLimiter(IMemoryCache cache)
        {
            _cache = cache;
        }

        public bool TryConsume(string key, int limit, TimeSpan window, out TimeSpan? retryAfter)
        {
            retryAfter = null;
            var cacheKey = $"rl:{key}";
            var now = DateTimeOffset.UtcNow;

            lock (_lock)
            {
                if (!_cache.TryGetValue(cacheKey, out RateLimitEntry? entry) || entry.ExpiresAt <= now)
                {
                    entry = new RateLimitEntry
                    {
                        Count = 0,
                        ExpiresAt = now.Add(window)
                    };
                }

                if (entry.Count >= limit)
                {
                    retryAfter = entry.ExpiresAt - now;
                    _cache.Set(cacheKey, entry, entry.ExpiresAt);
                    return false;
                }

                entry.Count++;
                _cache.Set(cacheKey, entry, entry.ExpiresAt);
                return true;
            }
        }

        private sealed class RateLimitEntry
        {
            public int Count { get; set; }
            public DateTimeOffset ExpiresAt { get; set; }
        }
    }
}
