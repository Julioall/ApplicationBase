using Application.Domain.Interface;
using Application.Domain.Interface.Students;
using Application.Domain.Interface.Education;
using Application.Infrastructure.Repository.Education;
using Application.Infrastructure.Repository;
using Application.Infrastructure.ConfigurationDb;
using Application.Infrastructure.Indexes;
using Application.Infrastructure.Repository.Students;
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
            services.TryAddScoped<ISettingsRepository, SettingsRepository>();
            services.TryAddScoped<IStudentRepository, StudentRepository>();
            services.TryAddScoped<IEducationRepository, EducationRepository>();
            services.TryAddScoped<IEducationImportRepository, EducationImportRepository>();
            services.TryAddScoped<INotificationRepository, NotificationRepository>();
            services.TryAddScoped<IWhatsAppInstanceRepository, WhatsAppInstanceRepository>();

            return services;
        }
    }
}
