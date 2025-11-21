using Application.Api;
using Application.Api.Filters;
using Application.Api.Middlewares;
using Application.Domain;
using Application.Domain.Model;
using Application.Infrastructure;
using Application.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

        // Service configuration
        builder.Services.AddScoped<ValidationProblemDetailsFilter>();
        builder.Services.AddControllers(options =>
        {
            options.Filters.AddService<ValidationProblemDetailsFilter>();
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = null;
            options.JsonSerializerOptions.DictionaryKeyPolicy = null;
        });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });
        builder.Services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[]
            {
                new CultureInfo("pt-BR"),
                new CultureInfo("pt"),
                new CultureInfo("en-US"),
                new CultureInfo("en")
            };

            options.DefaultRequestCulture = new RequestCulture("pt-BR");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
            options.ApplyCurrentCultureToResponseHeaders = true;
        });

        // Disable Swagger (if necessary)
        // If you do not want to activate it anymore, completely remove these lines
        // builder.Services.AddSwaggerGen(options =>
        // {
        //     options.SwaggerDoc("v1", new OpenApiInfo { Title = "Application API", Version = "v1" });
        //     options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        //     {
        //         Name = "Authorization",
        //         Type = SecuritySchemeType.ApiKey,
        //         Scheme = "Bearer",
        //         BearerFormat = "JWT",
        //         In = ParameterLocation.Header,
        //         Description = "Bearer Token Authentication"
        //     });
        //     options.AddSecurityRequirement(new OpenApiSecurityRequirement
        //     {
        //         {
        //             new OpenApiSecurityScheme
        //             {
        //                 Reference = new OpenApiReference
        //                 {
        //                     Type = ReferenceType.SecurityScheme,
        //                     Id = "Bearer"
        //                 }
        //             },
        //             new string[] {}
        //         }
        //     });
        // });

        // Add JWT configuration
        builder.Services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var issuer = Environment.GetEnvironmentVariable(ApplicationConstants.JWT_ISSUER_KEY);
            var audience = Environment.GetEnvironmentVariable(ApplicationConstants.JWT_AUDIENCE_KEY);
            var signingKey = Environment.GetEnvironmentVariable(ApplicationConstants.JWT_SIGNING_KEY);

            if (string.IsNullOrEmpty(issuer))
                throw new ArgumentNullException(nameof(issuer), "JWT issuer is not configured.");
            if (string.IsNullOrEmpty(audience))
                throw new ArgumentNullException(nameof(audience), "JWT audience is not configured.");
            if (string.IsNullOrEmpty(signingKey))
                throw new ArgumentNullException(nameof(signingKey), "JWT signing key is not configured.");

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
            };
        });

        // Register dependency injection modules
        DependencyInjectionModuleDomain.AddDomainDependencies(builder.Services);
        DependencyInjectionModuleInfra.AddInfraDependencies(builder.Services);
        DependencyInjectionModuleService.AddServiceDependencies(builder.Services);
        DependencyInjectionModuleWeb.AddWebDependencies(builder.Services);


        var app = builder.Build();

        var localizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
        app.UseRequestLocalization(localizationOptions);

        app.UseMiddleware<ProblemDetailsMiddleware>();
        app.UseMiddleware<MiddlewareServiceRavenDbStore>();
        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.UseCors(options =>
        {
            options.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });

        // Keep Swagger disabled in all environments
        // if (app.Environment.IsDevelopment())
        // {
        //     app.UseSwagger();
        //     app.UseSwaggerUI();
        // }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapFallbackToFile("/index.html");

        app.Run();
    }
}
