using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Application.Api.Health;

/// <summary>
/// Health check para validar a conexão com Redis.
/// Executado no startup (se CRÍTICO) ou periodicamente (se NÃO-CRÍTICO).
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;

    public RedisHealthCheck(IConnectionMultiplexer redis)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_redis.IsConnected)
            {
                return HealthCheckResult.Unhealthy("Redis is not connected");
            }

            // Tenta fazer ping no servidor
            var endpoints = _redis.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                var ping = await server.PingAsync();
                
                if (ping == TimeSpan.Zero)
                {
                    return HealthCheckResult.Unhealthy(
                        $"Redis endpoint {endpoint} failed ping test");
                }
            }

            return HealthCheckResult.Healthy("Redis is connected and responsive");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Redis health check failed: {ex.Message}", ex);
        }
    }
}
