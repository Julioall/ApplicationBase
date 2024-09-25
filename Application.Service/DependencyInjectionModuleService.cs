using Application.Domain.Interface;
using Application.Infrastructure.Repository;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Service
{
    public static class DependencyInjectionModuleService
    {
        public static void RegisterServices(IServiceCollection services)
        {
            services.AddRavenDB();
            services.AddScoped<IUserRepository, UserRepositoryRavenDB>();
        }
    }
}
