using System.Globalization;
using System.Text;
using System.Text.Json;
using Application.Api.Filters;
using Application.Api.Background;
using Application.Api.Health;
using Application.Api.Middlewares;
using Application.Domain;
using Application.Domain.Localization;
using Application.Domain.Model;
using Application.Infrastructure;
using Application.Service;
using Application.Shared.Background;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Application.Api.RateLimiting;
using Application.Api.Hangfire;
using Microsoft.Extensions.Logging;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Logging.ClearProviders();
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
        builder.Services.AddMemoryCache();
        builder.Services.Configure<RateLimitSettings>(builder.Configuration.GetSection("RateLimiting"));
        builder.Services.AddSingleton<IRateLimiter, MemoryRateLimiter>();

        var hangfireConnectionString = Environment.GetEnvironmentVariable(ApplicationConstants.HANGFIRE_CONNECTION_STRING_KEY);
        if (string.IsNullOrWhiteSpace(hangfireConnectionString))
        {
            throw new ArgumentNullException(nameof(hangfireConnectionString), SharedResourceProvider.GetString("EnvVarNotDefined", ApplicationConstants.HANGFIRE_CONNECTION_STRING_KEY));
        }

        builder.Services.AddHangfire(config =>
        {
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(hangfireConnectionString);
                });
        });
        builder.Services.AddHangfireServer();

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
                throw new ArgumentNullException(nameof(issuer), SharedResourceProvider.GetString("JwtIssuerNotConfigured"));
            if (string.IsNullOrEmpty(audience))
                throw new ArgumentNullException(nameof(audience), SharedResourceProvider.GetString("JwtAudienceNotConfigured"));
            if (string.IsNullOrEmpty(signingKey))
                throw new ArgumentNullException(nameof(signingKey), SharedResourceProvider.GetString("JwtSigningKeyNotConfigured"));

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

        builder.Services.AddAuthorization(options =>
        {
            foreach (var permission in ApplicationPermissions.All)
            {
                options.AddPolicy(permission, policy =>
                    policy.RequireClaim(ApplicationPermissions.PermissionClaimType, permission));
            }
        });

        builder.Services.AddHealthChecks()
            .AddCheck<StartupConfigurationHealthCheck>("startup_configuration", tags: new[] { "startup" });

        // Register dependency injection modules
        builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
        builder.Services.Configure<MoodleSettings>(builder.Configuration.GetSection("MoodleSettings"));
        DependencyInjectionModuleDomain.AddDomainDependencies(builder.Services);
        DependencyInjectionModuleInfra.AddInfraDependencies(builder.Services);
        DependencyInjectionModuleService.AddServiceDependencies(builder.Services);
        
        // Register Hangfire job for education import
        builder.Services.AddScoped<IEducationImportJob, EducationImportHangfireJob>();
        builder.Services.AddScoped<EducationImportHangfireJob>();
        builder.Services.AddScoped<IEducationReportImportJob, EducationReportImportHangfireJob>();
        builder.Services.AddScoped<EducationReportImportHangfireJob>();
        
        // NOTE: EducationImportBackgroundService is now replaced by Hangfire jobs (EducationImportHangfireJob)
        // The background service below can be kept as a fallback for missed imports, but Hangfire handles the main processing
        // builder.Services.AddHostedService<EducationImportBackgroundService>();


        var app = builder.Build();

        RunStartupValidation(app.Services);

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
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
        });

        app.MapHealthChecks("/health/startup", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("startup"),
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var payload = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(entry => new
                    {
                        name = entry.Key,
                        status = entry.Value.Status.ToString(),
                        description = entry.Value.Description
                    })
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
            }
        });

        app.MapControllers();
        app.MapFallbackToFile("/index.html");

        app.Run();
    }

    private static void RunStartupValidation(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var healthService = scope.ServiceProvider.GetRequiredService<HealthCheckService>();
        var report = healthService.CheckHealthAsync(registration => registration.Tags.Contains("startup"))
            .GetAwaiter().GetResult();

        if (report.Status != HealthStatus.Healthy)
        {
            var details = string.Join("; ", report.Entries.Select(entry =>
            {
                var description = string.IsNullOrWhiteSpace(entry.Value.Description)
                    ? entry.Value.Status.ToString()
                    : entry.Value.Description;
                return $"{entry.Key}: {description}";
            }));

            throw new InvalidOperationException(SharedResourceProvider.GetString("StartupValidationFailed", details));
        }
    }
}
