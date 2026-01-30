using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Polly;
using Application.Service.Resilience;
using Application.Service.CQRS;

namespace Application.Service.Behaviors
{
    /// <summary>
    /// MediatR Behavior que aplica políticas de resiliência automaticamente.
    /// 
    /// Padrão: Cada request é executado dentro de uma política Polly apropriada.
    /// - Commands: External API Policy (para mutations com risco de falha)
    /// - Queries: Repository Policy (para leituras simples)
    /// 
    /// Benefíciosdo padrão Behavior:
    /// 1. Zero boilerplate em handlers (políticas são transparentes)
    /// 2. Aplicável a todos os handlers automaticamente
    /// 3. Logging centralizado de tentativas e fallbacks
    /// 4. Fácil de testar e mockar
    /// </summary>
    public class ResiliencePolicyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IResiliencePolicyProvider _policyProvider;
        private readonly ILogger<ResiliencePolicyBehavior<TRequest, TResponse>> _logger;

        public ResiliencePolicyBehavior(
            IResiliencePolicyProvider policyProvider,
            ILogger<ResiliencePolicyBehavior<TRequest, TResponse>> logger)
        {
            _policyProvider = policyProvider;
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var requestTypeName = GetRequestType(request);

            _logger.LogInformation(
                "Iniciando execução de {RequestName} ({RequestType})",
                requestName,
                requestTypeName);

            try
            {
                var policy = GetPolicyForRequest(request);
                var stopwatch = Stopwatch.StartNew();

                // Wrapping da execução do handler dentro da política
                var result = await policy.ExecuteAsync(async (ct) =>
                {
                    return await next();
                }, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "Execução bem-sucedida de {RequestName} em {Duration}ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "Operação cancelada para {RequestName}",
                    requestName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao executar {RequestName}: {ErrorMessage}",
                    requestName,
                    ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Seleciona a política apropriada baseada no tipo de request.
        /// </summary>
        private IAsyncPolicy<TResponse> GetPolicyForRequest(TRequest request)
        {
            // Determina qual política usar baseado no tipo de request
            return request switch
            {
                // Se for um Command, usar External API Policy (mais tolerante a falhas)
                _ when typeof(TRequest).Name.EndsWith("Command") =>
                    _policyProvider.GetExternalApiPolicy<TResponse>(),

                // Se for uma Query cacheada, usar Repository Policy (mais rápida)
                _ when typeof(TRequest).GetInterfaces().Any(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICachedQuery<>)) =>
                    _policyProvider.GetRepositoryPolicy<TResponse>(),

                // Se for uma Query comum, usar Repository Policy
                _ when typeof(TRequest).Name.EndsWith("Query") =>
                    _policyProvider.GetRepositoryPolicy<TResponse>(),

                // Default: usar Repository Policy
                _ => _policyProvider.GetRepositoryPolicy<TResponse>()
            };
        }

        /// <summary>
        /// Determina o tipo de request (Command, Query, etc).
        /// </summary>
        private static string GetRequestType(TRequest request)
        {
            var interfaceNames = typeof(TRequest).GetInterfaces()
                .Select(i => i.Name)
                .ToList();

            if (interfaceNames.Contains("ICachedQuery") || interfaceNames.Any(n => n.StartsWith("ICachedQuery`")))
                return "CachedQuery";

            if (typeof(TRequest).Name.EndsWith("Command"))
                return "Command";

            if (typeof(TRequest).Name.EndsWith("Query"))
                return "Query";

            return "Unknown";
        }
    }
}
