using Application.Domain.Interface;
using Application.Infrastructure.ConfigurationDb;
using Application.Infrastructure.Interface;
using Application.Infrastructure.Repository;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using System.Linq;

namespace Application.Infrastructure
{
    public static class DependencyInjectionModuleInfra
    {
        public static IServiceCollection AddInfraDependencies(this IServiceCollection services)
        {
            if (!services.Any(sd => sd.ServiceType == typeof(IDocumentStore)))
            {
                services.AddSingleton<IDocumentStore>(_ =>
                {
                    var store = DocumentStoreHolderAlternative.CreateStore();
                    DocumentStoreHolderAlternative.CreateDatabaseIfDontExist(store.Database, true, store);
                    return store;
                });
            }

            services.AddScoped<IUserRepository, UserRepository>();

            return services;
        }
    }
}
