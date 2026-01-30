using Microsoft.Extensions.Diagnostics.HealthChecks;
using Raven.Client.Documents;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Web.Health
{
    /// <summary>
    /// Health check para RavenDB.
    /// Verifica se consegue conectar e fazer uma query simples.
    /// </summary>
    public class RavenDbHealthCheck : IHealthCheck
    {
        private readonly IDocumentStore _documentStore;

        public RavenDbHealthCheck(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Tenta fazer uma query simples para verificar conexão
                using (var session = _documentStore.OpenAsyncSession())
                {
                    // Query que não retorna dados, apenas verifica conexão
                    var stats = await session.Query<dynamic>()
                        .Statistics(out var statistics)
                        .Take(0)
                        .ToListAsync(cancellationToken);
                }

                return HealthCheckResult.Healthy(
                    "RavenDB está conectado e respondendo normalmente"
                );
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    "RavenDB não está disponível",
                    ex
                );
            }
        }
    }
}
