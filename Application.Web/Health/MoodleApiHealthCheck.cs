using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Web.Health
{
    /// <summary>
    /// Health check para Moodle API.
    /// Faz um request GET simples ao endpoint de versão do Moodle.
    /// </summary>
    public class MoodleApiHealthCheck : IHealthCheck
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MoodleApiHealthCheck> _logger;
        private readonly string _moodleBaseUrl;

        public MoodleApiHealthCheck(
            HttpClient httpClient,
            ILogger<MoodleApiHealthCheck> logger,
            string moodleBaseUrl = "http://moodle:80")
        {
            _httpClient = httpClient;
            _logger = logger;
            _moodleBaseUrl = moodleBaseUrl;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Tenta acessar a página de versão do Moodle
                var requestUrl = $"{_moodleBaseUrl}/web/version.json";
                
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Add("User-Agent", "ApplicationBase-HealthCheck/1.0");

                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    
                    _logger.LogInformation(
                        "Moodle API health check passou. " +
                        "Response: {Response}",
                        content[..100] // Primeiros 100 caracteres
                    );

                    return HealthCheckResult.Healthy(
                        "Moodle API está disponível"
                    );
                }
                else
                {
                    _logger.LogWarning(
                        "Moodle API retornou status {StatusCode}",
                        response.StatusCode
                    );

                    return HealthCheckResult.Degraded(
                        $"Moodle API retornou {response.StatusCode}",
                        null
                    );
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao conectar em Moodle API"
                );

                return HealthCheckResult.Unhealthy(
                    "Moodle API não está acessível",
                    ex
                );
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(
                    ex,
                    "Timeout ao conectar em Moodle API"
                );

                return HealthCheckResult.Unhealthy(
                    "Moodle API timeout",
                    ex
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro inesperado ao verificar Moodle API"
                );

                return HealthCheckResult.Unhealthy(
                    "Erro inesperado ao verificar Moodle",
                    ex
                );
            }
        }
    }
}
