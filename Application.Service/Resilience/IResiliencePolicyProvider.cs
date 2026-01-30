using Polly;

namespace Application.Service.Resilience
{
    /// <summary>
    /// Define políticas de resiliência para operações assíncronas.
    /// Implementa Retry, Circuit Breaker, Timeout e Fallback.
    /// </summary>
    public interface IResiliencePolicyProvider
    {
        /// <summary>
        /// Retorna uma política de resiliência para operações de repositório.
        /// Inclui retry com backoff exponencial, circuit breaker e timeout.
        /// </summary>
        IAsyncPolicy<T> GetRepositoryPolicy<T>();

        /// <summary>
        /// Retorna uma política de resiliência para chamadas a APIs externas.
        /// Mais agressiva com timeouts curtos e circuit breaker sensível.
        /// </summary>
        IAsyncPolicy<T> GetExternalApiPolicy<T>();

        /// <summary>
        /// Retorna uma política de resiliência para serviços de cache.
        /// Com fallback para bypass em caso de falha.
        /// </summary>
        IAsyncPolicy<T> GetCachePolicy<T>();

        /// <summary>
        /// Retorna uma política de resiliência para operações de banco de dados.
        /// Com timeout longo para operações pesadas.
        /// </summary>
        IAsyncPolicy<T> GetDatabasePolicy<T>();
    }
}
