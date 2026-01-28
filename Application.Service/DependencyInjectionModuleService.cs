using Application.Service.Interface;
using Application.Service.Service;
using Application.Service.Service.Moodle;
using Application.Service.Service.Security;
using Application.Service.Education;
using Application.Service.Education.Parsers;
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
            services.AddScoped<IStudentService, StudentService>();
            services.AddScoped<IEducationService, EducationService>();
            services.AddScoped<IExcelReportParser, ExcelReportParser>();
            services.AddScoped<IEducationReportImportProcessor, EducationReportImportProcessor>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<ITodoService, TodoService>();
            services.AddHttpClient<IMoodleCourseClient, MoodleCourseClient>();
            return services;
        }
    }
}
