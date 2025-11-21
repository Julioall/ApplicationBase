using Application.Domain.Model;
using Application.Infrastructure.ConfigurationDb;
using Application.Infrastructure.Interface;

namespace Application.Api.Middlewares
{
    public class MiddlewareServiceRavenDbStore
    {
        private readonly RequestDelegate _next;

        public MiddlewareServiceRavenDbStore(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext, IServiceRavenDB serviceRavenDB)
        {
            if (serviceRavenDB.Session is null)
            {
                var nameDatabase = Environment.GetEnvironmentVariable(ApplicationConstants.DATABASE_NAME_KEY);
                var store = DocumentStoreHolderAlternative.Store;
                serviceRavenDB.Store = store;
                serviceRavenDB.Session = store.OpenSession(nameDatabase);
                serviceRavenDB.AsyncSession = store.OpenAsyncSession(nameDatabase);
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
            }
        }
    }

}
