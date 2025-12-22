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
            services.AddScoped<IStudentService, StudentService>();
            services.AddScoped<IEducationService, EducationService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddHostedService<EducationImportBackgroundService>();
            return services;
        }
    }
}
