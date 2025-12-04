using Application.Service.Interface;
using Application.Service.Service;
using Application.Service.Service.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Service
{
    public static class DependencyInjectionModuleService
    {
        public static IServiceCollection AddServiceDependencies(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ISettingsService, SettingsService>();
            services.AddScoped<ISecretEncryptionService, SecretEncryptionService>();
            return services;
        }
    }
}
