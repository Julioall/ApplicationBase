using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Application.Service.CQRS;

namespace Application.Service.Behaviors
{
    /// <summary>
    /// MediatR Behavior que implementa caching de queries.
    /// 
    /// Padrão: Cache-Aside (Lazy Loading)
    /// 1. Verifica se resultado existe em cache
    /// 2. Se sim, retorna do cache
    /// 3. Se não, executa handler
    /// 4. Armazena resultado em cache com TTL configurado
    /// 
    /// Escopo: Apenas queries que implementam ICachedQuery
    /// 
    /// Benefícios:
    /// - Reduz latência para queries frequentes
    /// - Reduz carga no banco de dados
    /// - Transparente para handlers (zero mudanças)
    /// - Fácil de configurar por query (CacheDurationSeconds)
    /// </summary>
    public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IDistributedCacheService? _cacheService;
        private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

        public CachingBehavior(
            IDistributedCacheService? cacheService,
            ILogger<CachingBehavior<TRequest, TResponse>> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Se cache não está disponível ou request não é cacheável, apenas execute
            if (_cacheService == null || !IsCacheableQuery(request))
            {
                return await next();
            }

            var requestName = typeof(TRequest).Name;
            var cacheKey = GenerateCacheKey(request);
            var cacheDuration = GetCacheDuration(request);

            _logger.LogInformation(
                "Verificando cache para {RequestName} com chave {CacheKey}",
                requestName,
                cacheKey);

            try
            {
                // Tenta obter do cache
                var cachedResult = await _cacheService.GetAsync<TResponse>(cacheKey);

                if (cachedResult != null)
                {
                    _logger.LogInformation(
                        "Cache HIT para {RequestName}",
                        requestName);

                    return cachedResult;
                }

                _logger.LogInformation(
                    "Cache MISS para {RequestName}, executando handler",
                    requestName);

                // Cache miss: executar handler
                var stopwatch = Stopwatch.StartNew();
                var result = await next();
                stopwatch.Stop();

                // Armazenar no cache
                await _cacheService.SetAsync(
                    cacheKey,
                    result,
                    TimeSpan.FromSeconds(cacheDuration));

                _logger.LogInformation(
                    "Resultado de {RequestName} cacheado por {CacheDuration}s (execução levou {Duration}ms)",
                    requestName,
                    cacheDuration,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Erro ao acessar cache para {RequestName}, executando handler sem cache",
                    requestName);

                // Se cache falhar, apenas executa normalmente
                return await next();
            }
        }

        /// <summary>
        /// Verifica se a request é cacheável.
        /// </summary>
        private static bool IsCacheableQuery(TRequest request)
        {
            var interfaces = typeof(TRequest).GetInterfaces();

            // Verifica se implementa ICachedQuery ou ICachedQuery<T>
            return interfaces.Any(i =>
                i.Name == "ICachedQuery" ||
                (i.IsGenericType && i.GetGenericTypeDefinition().Name == "ICachedQuery`1"));
        }

        /// <summary>
        /// Gera chave de cache baseada no tipo de request e seus parâmetros.
        /// </summary>
        private static string GenerateCacheKey(TRequest request)
        {
            // Se a request tem uma chave customizada, usar ela
            if (request is ICachedQuery cachedQuery && !string.IsNullOrEmpty(cachedQuery.CacheKey))
            {
                return cachedQuery.CacheKey;
            }

            // Default: usar nome do tipo + hash dos parâmetros
            var requestType = typeof(TRequest).Name;
            var requestJson = JsonSerializer.Serialize(request);
            var parameterHash = GetStringHash(requestJson);

            return $"cached_{requestType}_{parameterHash}";
        }

        /// <summary>
        /// Obtém a duração do cache em segundos.
        /// </summary>
        private static int GetCacheDuration(TRequest request)
        {
            if (request is ICachedQuery cachedQuery)
            {
                return cachedQuery.CacheDurationSeconds;
            }

            // Default: 5 minutos
            return 300;
        }

        /// <summary>
        /// Gera um hash curto para a string (usado em chaves de cache).
        /// </summary>
        private static string GetStringHash(string input)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(hashedBytes).Substring(0, 8); // Usar primeiros 8 chars
            }
        }
    }
}
