using Application.Domain.Interface;
using Application.Infrastructure.ConfigurationDb;
using Application.Infrastructure.Indexes;
using Application.Infrastructure.Repository;
using Application.Infrastructure.Service;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
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
                    DocumentStoreHolderAlternative.CreateDatabaseIfDontExist(store, store.Database, true);
                    IndexCreation.CreateIndexes(typeof(User_ByEmail).Assembly, store);
                    return store;
                });
            }

            services.AddScoped<IServiceRavenDB, ServiceRavenDB>();
            services.AddScoped<IUserRepository, UserRepository>();

            return services;
        }
    }
}
