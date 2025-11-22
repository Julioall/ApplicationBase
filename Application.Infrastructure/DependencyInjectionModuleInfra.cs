using Application.Domain.Interface;
using Application.Infrastructure.ConfigurationDb;
using Application.Infrastructure.Indexes;
using Application.Infrastructure.Repository;
using Application.Infrastructure.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;

namespace Application.Infrastructure
{
    public static class DependencyInjectionModuleInfra
    {
        public static IServiceCollection AddInfraDependencies(this IServiceCollection services)
        {
            services.TryAddSingleton<IDocumentStore>(_ =>
            {
                var store = DocumentStoreHolderAlternative.CreateStore();
                DocumentStoreHolderAlternative.CreateDatabaseIfDontExist(store, store.Database, true);
                IndexCreation.CreateIndexes(typeof(User_ByEmail).Assembly, store);
                return store;
            });

            services.TryAddScoped<IServiceRavenDB, ServiceRavenDB>();
            services.TryAddScoped<IUserRepository, UserRepository>();

            return services;
        }
    }
}
