using Application.Domain.Interface;
using Application.Domain.Model;
using Application.Infrastructure.ConfigurationDb;
using Application.Infrastructure.Persistence;

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
                var nameDatabase = ApplicationConstants.DATABASE_NAME;
                serviceRavenDB.Session = DocumentStoreHolderAlternative.Store.OpenSession(nameDatabase);
                serviceRavenDB.AsyncSession = DocumentStoreHolderAlternative.Store.OpenAsyncSession(nameDatabase);
                serviceRavenDB.Store = DocumentStoreHolderAlternative.Store;
            }

            await _next(httpContext);

            if (serviceRavenDB.AsyncSession != null)
            {
                await serviceRavenDB.AsyncSession.SaveChangesAsync();
            }
        }
    }

}
