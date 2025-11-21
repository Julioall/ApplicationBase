using Application.Domain.Model.Dtos;
using Application.Domain.Model.User;
using Application.Domain.Validator;
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
            return services;
        }
    }
}
