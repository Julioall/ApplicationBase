using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Domain.Localization;
using Application.Domain.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Application.Api.Health
{
    /// <summary>
    /// Validates critical environment variables needed for the app to start safely.
    /// </summary>
    public class StartupConfigurationHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var issues = new List<string>();

            var signingKey = Environment.GetEnvironmentVariable(ApplicationConstants.JWT_SIGNING_KEY);
            if (string.IsNullOrWhiteSpace(signingKey))
            {
                issues.Add(SharedResourceProvider.GetString("JwtSigningKeyNotConfigured"));
            }

            var encryptionKey = Environment.GetEnvironmentVariable(ApplicationConstants.SECRET_ENCRYPTION_KEY);
            if (string.IsNullOrWhiteSpace(encryptionKey))
            {
                issues.Add(SharedResourceProvider.GetString("EnvVarNotDefined", ApplicationConstants.SECRET_ENCRYPTION_KEY));
            }

            var ravenUrlsRaw = Environment.GetEnvironmentVariable(ApplicationConstants.DATABASE_URL_KEY);
            if (string.IsNullOrWhiteSpace(ravenUrlsRaw))
            {
                issues.Add(SharedResourceProvider.GetString("EnvVarNotDefined", ApplicationConstants.DATABASE_URL_KEY));
            }
            else
            {
                var ravenUrls = ravenUrlsRaw.Split(',')
                    .Select(url => url.Trim())
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .ToList();

                if (ravenUrls.Count == 0)
                {
                    issues.Add(SharedResourceProvider.GetString("EnvVarNotDefined", ApplicationConstants.DATABASE_URL_KEY));
                }
                else
                {
                    var invalidUrls = ravenUrls
                        .Where(url => !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                                      (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                        .ToList();

                    if (invalidUrls.Count > 0)
                    {
                        issues.Add(SharedResourceProvider.GetString("InvalidRavenDbUrls", string.Join(", ", invalidUrls)));
                    }
                }
            }

            if (issues.Count > 0)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(string.Join("; ", issues)));
            }

            return Task.FromResult(HealthCheckResult.Healthy(SharedResourceProvider.GetString("StartupValidationPassed")));
        }
    }
}
