using Application.Service.Interface;
using Application.Service.Service;
using Application.Service.Service.Moodle;
using Application.Service.Service.Security;
using Application.Service.Resilience;
using MediatR;
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
            services.AddHttpClient<IMoodleAuthClient, MoodleAuthClient>();
            services.AddHttpClient<EvolutionWhatsAppInstanceProvider>();
            services.AddScoped<IWhatsAppInstanceProvider, EvolutionWhatsAppInstanceProvider>();
            services.AddScoped<IWhatsAppInstanceService, WhatsAppInstanceService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<ITodoService, TodoService>();
            
            // Phase 2: Resilience (Polly)
            services.AddSingleton<IResiliencePolicyProvider, ResiliencePolicyProvider>();
            
            // Phase 1: CQRS (MediatR)
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjectionModuleService).Assembly));
            
            return services;
        }
    }
}
