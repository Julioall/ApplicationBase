using System;
using System.Threading.Tasks;

namespace Application.Service.Behaviors
{
    /// <summary>
    /// Interface para serviço de cache distribuído.
    /// Permite abstrair a implementação (Redis, Memcached, In-Memory, etc).
    /// </summary>
    public interface IDistributedCacheService
    {
        /// <summary>
        /// Obtém um valor do cache.
        /// </summary>
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Armazena um valor no cache com TTL.
        /// </summary>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove um valor do cache.
        /// </summary>
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove múltiplos valores do cache.
        /// </summary>
        Task RemoveAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica se uma chave existe no cache.
        /// </summary>
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Limpa todo o cache (use com cuidado).
        /// </summary>
        Task FlushAsync(CancellationToken cancellationToken = default);
    }
}
