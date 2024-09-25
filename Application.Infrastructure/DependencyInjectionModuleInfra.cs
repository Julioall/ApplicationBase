using Application.Domain.Interface;
using Application.Infrastructure.Repository;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Infrastructure
{
    public static class DependencyInjectionModuleInfra
    {
        public static void RegisterServices(IServiceCollection services)
        {

            services.AddRavenDB();
            services.AddScoped<IUserRepository, UserRepositoryRavenDB>();
        }
    }
}
