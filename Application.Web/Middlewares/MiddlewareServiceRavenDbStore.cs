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
