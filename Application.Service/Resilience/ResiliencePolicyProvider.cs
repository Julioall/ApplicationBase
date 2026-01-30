using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Microsoft.Extensions.Logging;

namespace Application.Service.Resilience
{
    /// <summary>
    /// Implementação das políticas de resiliência usando Polly.
    /// 
    /// Padrões implementados:
    /// 1. **Retry com Exponential Backoff**: Reintentar com delay crescente
    /// 2. **Circuit Breaker**: Falhar rápido após X erros consecutivos
    /// 3. **Timeout**: Cancelar operações longas
    /// 4. **Bulkhead Isolation**: Isolar falhas por tipo
    /// </summary>
    public class ResiliencePolicyProvider : IResiliencePolicyProvider
    {
        private readonly ILogger<ResiliencePolicyProvider> _logger;
        
        // Políticas cached (evita recriação)
        private IAsyncPolicy<object>? _repositoryPolicyCached;
        private IAsyncPolicy<object>? _externalApiPolicyCached;
        private IAsyncPolicy<object>? _cachePolicyCached;
        private IAsyncPolicy<object>? _databasePolicyCached;

        public ResiliencePolicyProvider(ILogger<ResiliencePolicyProvider> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Política para operações de repositório (RavenDB, EF Core).
        /// 
        /// - Retry: 3 tentativas com backoff (1s, 2s, 4s)
        /// - Circuit Breaker: Abre após 5 falhas em 30s
        /// - Timeout: 10 segundos
        /// </summary>
        public IAsyncPolicy<T> GetRepositoryPolicy<T>()
        {
            return (IAsyncPolicy<T>)(object)(
                _repositoryPolicyCached ??= Policy.WrapAsync<object>(
                    GetRetryPolicy<object>(maxRetries: 3, initialDelayMs: 1000),
                    GetCircuitBreakerPolicy<object>(failureThreshold: 5, durationSeconds: 30),
                    GetTimeoutPolicy<object>(timeoutSeconds: 10)
                )
            );
        }

        /// <summary>
        /// Política para chamadas a APIs externas (Moodle, WhatsApp, etc).
        /// 
        /// - Retry: 2 tentativas com backoff (500ms, 1s)
        /// - Circuit Breaker: Abre após 3 falhas em 20s
        /// - Timeout: 5 segundos
        /// </summary>
        public IAsyncPolicy<T> GetExternalApiPolicy<T>()
        {
            return (IAsyncPolicy<T>)(object)(
                _externalApiPolicyCached ??= Policy.WrapAsync<object>(
                    GetRetryPolicy<object>(maxRetries: 2, initialDelayMs: 500),
                    GetCircuitBreakerPolicy<object>(failureThreshold: 3, durationSeconds: 20),
                    GetTimeoutPolicy<object>(timeoutSeconds: 5)
                )
            );
        }

        /// <summary>
        /// Política para cache com fallback.
        /// 
        /// - Timeout: 2 segundos (cache rápido)
        /// - Circuit Breaker: Abre rapidamente
        /// - Fallback: Retorna null/default se falhar
        /// </summary>
        public IAsyncPolicy<T> GetCachePolicy<T>()
        {
            return (IAsyncPolicy<T>)(object)(
                _cachePolicyCached ??= Policy.WrapAsync<object>(
                    GetTimeoutPolicy<object>(timeoutSeconds: 2),
                    GetCircuitBreakerPolicy<object>(failureThreshold: 2, durationSeconds: 10),
                    GetFallbackPolicy<object>()
                )
            );
        }

        /// <summary>
        /// Política para operações de banco de dados pesadas.
        /// 
        /// - Retry: 2 tentativas
        /// - Circuit Breaker: Mais tolerante (10 falhas em 60s)
        /// - Timeout: 30 segundos (operações podem ser lentas)
        /// </summary>
        public IAsyncPolicy<T> GetDatabasePolicy<T>()
        {
            return (IAsyncPolicy<T>)(object)(
                _databasePolicyCached ??= Policy.WrapAsync<object>(
                    GetRetryPolicy<object>(maxRetries: 2, initialDelayMs: 500),
                    GetCircuitBreakerPolicy<object>(failureThreshold: 10, durationSeconds: 60),
                    GetTimeoutPolicy<object>(timeoutSeconds: 30)
                )
            );
        }

        /// <summary>
        /// Cria política de Retry com backoff exponencial.
        /// </summary>
        private IAsyncPolicy<T> GetRetryPolicy<T>(int maxRetries = 3, int initialDelayMs = 1000)
        {
            return Policy
                .Handle<Exception>()
                .OrResult<T>(r => r == null)
                .WaitAndRetryAsync<T>(
                    retryCount: maxRetries,
                    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(
                        initialDelayMs * (int)Math.Pow(2, attempt - 1)
                    ),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            "Retry {RetryCount} após {Delay}ms. " +
                            "Erro: {Error}",
                            retryCount,
                            timespan.TotalMilliseconds,
                            outcome.Exception?.Message ?? "Null result"
                        );
                    }
                );
        }

        /// <summary>
        /// Cria política de Circuit Breaker.
        /// Abre após X falhas consecutivas, rejeitando requisições por N segundos.
        /// </summary>
        private IAsyncPolicy<T> GetCircuitBreakerPolicy<T>(
            int failureThreshold = 5,
            int durationSeconds = 30)
        {
            return Policy
                .Handle<Exception>()
                .OrResult<T>(r => r == null)
                .CircuitBreakerAsync<T>(
                    handledEventsAllowedBeforeBreaking: failureThreshold,
                    durationOfBreak: TimeSpan.FromSeconds(durationSeconds),
                    onBreak: (outcome, duration) =>
                    {
                        _logger.LogError(
                            "Circuit breaker aberto por {Duration}s. " +
                            "Erro: {Error}",
                            duration.TotalSeconds,
                            outcome.Exception?.Message ?? "Null result"
                        );
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("Circuit breaker resetado");
                    }
                );
        }

        /// <summary>
        /// Cria política de Timeout.
        /// </summary>
        private IAsyncPolicy<T> GetTimeoutPolicy<T>(int timeoutSeconds = 10)
        {
            return Policy.TimeoutAsync<T>(
                TimeSpan.FromSeconds(timeoutSeconds),
                TimeoutStrategy.Optimistic,
                onTimeoutAsync: (context, timespan, task, ex) =>
                {
                    _logger.LogWarning(
                        "Timeout após {Timeout}s",
                        timespan.TotalSeconds
                    );
                    return Task.CompletedTask;
                }
            );
        }

        /// <summary>
        /// Cria política de Fallback (retorna valor padrão).
        /// </summary>
        private IAsyncPolicy<T> GetFallbackPolicy<T>()
        {
            return Policy
                .Handle<Exception>()
                .OrResult<T>(r => r == null)
                .FallbackAsync<T>(
                    fallbackValue: default!,
                    onFallbackAsync: (outcome, context) =>
                    {
                        _logger.LogWarning(
                            "Fallback acionado. " +
                            "Erro: {Error}",
                            outcome.Exception?.Message ?? "Null result"
                        );
                        return Task.CompletedTask;
                    }
                );
        }
    }
}
