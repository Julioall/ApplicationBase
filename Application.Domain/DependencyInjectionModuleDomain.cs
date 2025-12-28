using Application.Domain.Model.Dtos;
using Application.Domain.Model.Students;
using Application.Domain.Model.User;
using Application.Domain.Validator;
using Application.Domain.Validation;
using Application.Domain.Validation.Students;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Domain
{
    public static class DependencyInjectionModuleDomain
    {
        public static IServiceCollection AddDomainDependencies(this IServiceCollection services)
        {
            services.AddScoped<IValidator<User>, UserValidator>();
            services.AddScoped<IValidator<PasswordInput>, PasswordValidator>();
            services.AddScoped<IValidator<Student>, StudentValidator>();
            services.AddScoped<IValidator<WhatsAppSettingsRequest>, WhatsAppSettingsRequestValidator>();
            services.AddScoped<IValidator<CreateWhatsAppInstanceRequest>, CreateWhatsAppInstanceRequestValidator>();
            services.AddScoped<IValidator<UpdateWhatsAppInstanceRequest>, UpdateWhatsAppInstanceRequestValidator>();
            return services;
        }
    }
}
