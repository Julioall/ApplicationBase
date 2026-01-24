using System;
using Application.Domain.Model;
using Application.Infrastructure.Service;
using Raven.Client.Documents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Api.Middlewares
{
    public class MiddlewareServiceRavenDbStore
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<MiddlewareServiceRavenDbStore> _logger;

        public MiddlewareServiceRavenDbStore(RequestDelegate next, ILogger<MiddlewareServiceRavenDbStore> logger)
        {
            _next = next;
            _logger = logger;
        }

        // Legacy constructor for tests/backward compatibility
        public MiddlewareServiceRavenDbStore(RequestDelegate next)
        {
            _next = next;
            _logger = NullLogger<MiddlewareServiceRavenDbStore>.Instance;
        }

        public async Task Invoke(HttpContext httpContext, IServiceRavenDB serviceRavenDB, IDocumentStore documentStore)
        {
            if (serviceRavenDB.Session is null)
            {
                var nameDatabase = Environment.GetEnvironmentVariable(ApplicationConstants.DATABASE_NAME_KEY);
                var dbNameToUse = string.IsNullOrWhiteSpace(nameDatabase) ? documentStore.Database : nameDatabase;
                serviceRavenDB.Store = documentStore;
                serviceRavenDB.Session = documentStore.OpenSession(dbNameToUse);
                serviceRavenDB.AsyncSession = documentStore.OpenAsyncSession(dbNameToUse);
            }

            var maxRequests = documentStore.Conventions.MaxNumberOfRequestsPerSession;

            try
            {
                await _next(httpContext);

                serviceRavenDB.Session?.SaveChanges();
                if (serviceRavenDB.AsyncSession != null)
                {
                    await serviceRavenDB.AsyncSession.SaveChangesAsync();
                }
            }
            finally
            {
                LogRequestUsage(serviceRavenDB, maxRequests);

                serviceRavenDB.Session?.Dispose();
                if (serviceRavenDB.AsyncSession is IAsyncDisposable asyncSessionDisposable)
                {
                    await asyncSessionDisposable.DisposeAsync();
                }
                else
                {
                    serviceRavenDB.AsyncSession?.Dispose();
                }

                if (serviceRavenDB is IAsyncDisposable serviceDisposable)
                {
                    await serviceDisposable.DisposeAsync();
                }
                else if (serviceRavenDB is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        private void LogRequestUsage(IServiceRavenDB serviceRavenDB, int maxRequests)
        {
            if (maxRequests <= 0)
            {
                return;
            }

            var threshold = Math.Max(1, (int)(maxRequests * 0.8));

            var syncRequests = serviceRavenDB.Session?.Advanced.NumberOfRequests ?? 0;
            if (syncRequests >= threshold)
            {
                _logger.LogWarning("RavenDB sync session used {Requests} requests (threshold {Threshold}/{Max})", syncRequests, threshold, maxRequests);
            }

            var asyncRequests = serviceRavenDB.AsyncSession?.Advanced.NumberOfRequests ?? 0;
            if (asyncRequests >= threshold)
            {
                _logger.LogWarning("RavenDB async session used {Requests} requests (threshold {Threshold}/{Max})", asyncRequests, threshold, maxRequests);
            }
        }
    }

}
