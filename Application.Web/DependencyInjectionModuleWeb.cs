using Application.Domain.Interface;
using Application.Infrastructure.Repository;
using Application.Service.Interface;
using Application.Service.Service;

namespace Application.Api
{
    public static class DependencyInjectionModuleWeb
    {
        public static void RegisterServices(IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IUserRepository, UserRepositoryRavenDB>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ITokenService, TokenService>();
        }
    }
}
