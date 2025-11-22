using Application.Domain.Model;
using Application.Infrastructure.Interface;
using Raven.Client.Documents;

namespace Application.Api.Middlewares
{
    public class MiddlewareServiceRavenDbStore
    {
        private readonly RequestDelegate _next;

        public MiddlewareServiceRavenDbStore(RequestDelegate next)
        {
            _next = next;
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
    }

}
