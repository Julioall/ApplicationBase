using Application.Service.Behaviors;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Application.Infrastructure.Service;

/// <summary>
/// Implementação de cache distribuído usando Redis.
/// Substitui InMemoryCacheService para ambientes de produção distribuídos.
/// </summary>
public class RedisDistributedCacheService : IDistributedCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisDistributedCacheService> _logger;
    private readonly IDatabase _db;

    public RedisDistributedCacheService(
        IConnectionMultiplexer redis,
        ILogger<RedisDistributedCacheService> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _db = _redis.GetDatabase();
    }

    /// <summary>
    /// Obtém um valor do cache Redis.
    /// </summary>
    /// <typeparam name="T">Tipo do valor armazenado</typeparam>
    /// <param name="key">Chave do cache</param>
    /// <param name="cancellationToken">Cancellation token (não utilizado em Redis)</param>
    /// <returns>Valor deserializado ou null se não encontrado</returns>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning("Cache key is null or empty");
            return default;
        }

        try
        {
            var value = await _db.StringGetAsync(key);
            
            if (value.IsNullOrEmpty)
            {
                _logger.LogDebug("Cache miss for key: {CacheKey}", key);
                return default;
            }

            var deserialized = JsonSerializer.Deserialize<T>(value.ToString());
            _logger.LogDebug("Cache hit for key: {CacheKey}", key);
            return deserialized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cache for key: {CacheKey}", key);
            // Falha silenciosa: retorna null e continua execução
            return default;
        }
    }

    /// <summary>
    /// Armazena um valor no cache Redis.
    /// </summary>
    /// <typeparam name="T">Tipo do valor a armazenar</typeparam>
    /// <param name="key">Chave do cache</param>
    /// <param name="value">Valor a armazenar</param>
    /// <param name="expiration">Tempo de expiração (opcional)</param>
    /// <param name="cancellationToken">Cancellation token (não utilizado em Redis)</param>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning("Cache key is null or empty, skipping set");
            return;
        }

        if (value == null)
        {
            _logger.LogWarning("Cache value is null, skipping set for key: {CacheKey}", key);
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(key, json, expiration);
            _logger.LogDebug("Cache set for key: {CacheKey} with expiration: {Expiration}", 
                key, expiration?.TotalSeconds ?? -1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key: {CacheKey}", key);
            // Falha silenciosa: cache não é crítico para operação
        }
    }

    /// <summary>
    /// Remove um valor do cache Redis.
    /// </summary>
    /// <param name="key">Chave do cache</param>
    /// <param name="cancellationToken">Cancellation token (não utilizado em Redis)</param>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning("Cache key is null or empty, skipping remove");
            return;
        }

        try
        {
            await _db.KeyDeleteAsync(key);
            _logger.LogDebug("Cache key removed: {CacheKey}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {CacheKey}", key);
            // Falha silenciosa
        }
    }

    /// <summary>
    /// Remove múltiplos valores do cache Redis.
    /// </summary>
    /// <param name="keys">Chaves a remover</param>
    /// <param name="cancellationToken">Cancellation token (não utilizado em Redis)</param>
    public async Task RemoveAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        var keyList = keys?.Where(k => !string.IsNullOrWhiteSpace(k)).ToList() ?? new List<string>();
        if (!keyList.Any())
        {
            return;
        }

        try
        {
            var redisKeys = keyList.Select(k => (RedisKey)k).ToArray();
            await _db.KeyDeleteAsync(redisKeys);
            _logger.LogDebug("Removed {Count} cache keys", keyList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache keys");
        }
    }

    /// <summary>
    /// Verifica se uma chave existe no cache Redis.
    /// </summary>
    /// <param name="key">Chave a verificar</param>
    /// <param name="cancellationToken">Cancellation token (não utilizado em Redis)</param>
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        try
        {
            return await _db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if cache key exists: {CacheKey}", key);
            return false;
        }
    }

    /// <summary>
    /// Limpa todo o cache Redis (PERIGOSO EM PRODUÇÃO).
    /// Usar apenas em testes ou operações administrativas.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token (não utilizado em Redis)</param>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoints = _redis.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                await server.FlushDatabaseAsync();
            }
            _logger.LogWarning("All cache cleared - use with caution!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all cache");
        }
    }

    /// <summary>
    /// Verifica se o Redis está conectado.
    /// </summary>
    public bool IsConnected => _redis.IsConnected;
}

