using MediatR;

namespace Application.Service.CQRS
{
    /// <summary>
    /// Marker interface para queries que podem ser cacheadas.
    /// Usado pelo CachingBehavior para decidir se deve cachear a resposta.
    /// </summary>
    public interface ICachedQuery : IRequest
    {
        /// <summary>
        /// Tempo de vida do cache em segundos.
        /// </summary>
        int CacheDurationSeconds { get; }

        /// <summary>
        /// Chave customizada para o cache (opcional).
        /// Se não especificada, será gerada automaticamente.
        /// </summary>
        string? CacheKey { get; }
    }

    /// <summary>
    /// Marker interface genérica para queries cacheáveis com resposta tipada.
    /// </summary>
    public interface ICachedQuery<TResponse> : IRequest<TResponse>
    {
        /// <summary>
        /// Tempo de vida do cache em segundos.
        /// </summary>
        int CacheDurationSeconds { get; }

        /// <summary>
        /// Chave customizada para o cache (opcional).
        /// Se não especificada, será gerada automaticamente.
        /// </summary>
        string? CacheKey { get; }
    }
}
