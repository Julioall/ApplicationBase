using Application.Domain.Interface;
using Application.Infrastructure.Persistence;
using Application.Infrastructure.Repository;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Infrastructure
{
    public static class DependencyInjectionModuleInfra
    {
        public static IServiceCollection AddInfraDependencies(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();

            return services;
        }
    }
}
