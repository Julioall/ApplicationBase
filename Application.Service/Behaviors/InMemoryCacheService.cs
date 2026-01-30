using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Application.Service.Behaviors
{
    /// <summary>
    /// Implementação de cache em memória usando IMemoryCache.
    /// 
    /// Uso: Usar como fallback quando Redis não está disponível.
    /// Nota: Cache não é distribuído (apenas em processo).
    /// </summary>
    public class InMemoryCacheService : IDistributedCacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<InMemoryCacheService> _logger;

        public InMemoryCacheService(
            IMemoryCache memoryCache,
            ILogger<InMemoryCacheService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Obtendo chave '{Key}' do cache em memória", key);
            _memoryCache.TryGetValue(key, out T? value);
            return Task.FromResult(value);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            var options = new MemoryCacheEntryOptions();

            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
                _logger.LogDebug(
                    "Armazenando chave '{Key}' no cache em memória com expiração {Duration}s",
                    key,
                    expiration.Value.TotalSeconds);
            }
            else
            {
                _logger.LogDebug("Armazenando chave '{Key}' no cache em memória (sem expiração)", key);
            }

            _memoryCache.Set(key, value, options);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Removendo chave '{Key}' do cache em memória", key);
            _memoryCache.Remove(key);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            foreach (var key in keys)
            {
                _memoryCache.Remove(key);
            }

            _logger.LogDebug("Removidas {Count} chaves do cache em memória", keys.Count());
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_memoryCache.TryGetValue(key, out _));
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("Limpando todo o cache em memória");
            // Nota: IMemoryCache não expõe método para limpar tudo
            // Esta é uma limitação da interface
            return Task.CompletedTask;
        }
    }
}
