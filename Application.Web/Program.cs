using System.Globalization;
using System.Text;
using System.Text.Json;
using Application.Api.Filters;
using Application.Api.Health;
using Application.Web.Health;
using Application.Api.Middlewares;
using Application.Domain;
using Application.Domain.Localization;
using Application.Domain.Model;
using Application.Infrastructure;
using Application.Infrastructure.Service;
using Application.Service;
using Hangfire;
using Hangfire.PostgreSql;
using MediatR;
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
using Prometheus;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using StackExchange.Redis;

public class Program
{
    public static void Main(string[] args)
    {
        // Carregar variáveis de .env (desenvolvimento local)
        LoadEnvironmentVariables();
        
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production")
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/application-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                fileSizeLimitBytes: 10485760,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Iniciando aplicação...");
            
            var builder = WebApplication.CreateBuilder(args);
            
            // Use Serilog
            builder.Host.UseSerilog(Log.Logger, dispose: true);

        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
        builder.Services.AddMemoryCache();
        builder.Services.Configure<RateLimitSettings>(builder.Configuration.GetSection("RateLimiting"));
        builder.Services.AddSingleton<IRateLimiter, MemoryRateLimiter>();

        // Redis Configuration
        var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") 
            ?? "redis:6379";
        Log.Information("Connecting to Redis at {RedisConnection}", redisConnectionString);
        
        var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
        redisOptions.AbortOnConnectFail = false; // Permite fallback para in-memory em caso de falha
        
        var redis = ConnectionMultiplexer.Connect(redisOptions);
        builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
        
        // Registrar RedisDistributedCacheService
        if (redis.IsConnected)
        {
            Log.Information("Redis connected successfully");
            builder.Services.AddScoped<Application.Service.Behaviors.IDistributedCacheService,
                RedisDistributedCacheService>();
        }
        else
        {
            Log.Warning("Redis connection failed, falling back to InMemoryCacheService");
            builder.Services.AddScoped<Application.Service.Behaviors.IDistributedCacheService,
                Application.Service.Behaviors.InMemoryCacheService>();
        }

        var hangfireConnectionString = Environment.GetEnvironmentVariable(ApplicationConstants.HANGFIRE_CONNECTION_STRING_KEY);
        if (string.IsNullOrWhiteSpace(hangfireConnectionString))
        {
            if (builder.Environment.IsDevelopment())
            {
                // Fallback para desenvolvimento local
                Log.Warning("HANGFIRE_CONNECTION_STRING não definida. Usando valor padrão para desenvolvimento.");
                hangfireConnectionString = "Host=localhost;Port=5432;Database=evolution;Username=evolution;Password=evolution";
            }
            else
            {
                throw new ArgumentNullException(nameof(hangfireConnectionString), SharedResourceProvider.GetString("EnvVarNotDefined", ApplicationConstants.HANGFIRE_CONNECTION_STRING_KEY));
            }
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

        // Register MediatR with Behaviors
        Log.Information("Registrando MediatR handlers e behaviors...");
        builder.Services.AddMediatR(options =>
        {
            options.RegisterServicesFromAssembly(typeof(Program).Assembly);
            // Registrar behaviors na ordem: Prometheus -> Caching -> Resiliência
            // Prometheus PRIMEIRO para capturar todas as métricas
            options.AddOpenBehavior(typeof(Application.Service.Behaviors.PrometheusMetricsBehavior<,>));
            // Caching é executado DEPOIS de Prometheus
            options.AddOpenBehavior(typeof(Application.Service.Behaviors.CachingBehavior<,>));
            // Resiliência é executada POR ÚLTIMO
            options.AddOpenBehavior(typeof(Application.Service.Behaviors.ResiliencePolicyBehavior<,>));
        });

        // Registrar Prometheus metrics
        Log.Information("Registrando Prometheus metrics...");
        builder.Services.AddSingleton<CollectorRegistry>(Metrics.DefaultRegistry);

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
            .AddCheck<StartupConfigurationHealthCheck>("startup_configuration", tags: new[] { "startup" })
            .AddCheck<RavenDbHealthCheck>("ravendb", tags: new[] { "database" })
            .AddCheck<RedisHealthCheck>("redis", tags: new[] { "cache" })
            .AddCheck<MoodleApiHealthCheck>("moodle_api", tags: new[] { "external" });

        // Register dependency injection modules
        builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
        builder.Services.Configure<MoodleSettings>(builder.Configuration.GetSection("MoodleSettings"));
        DependencyInjectionModuleDomain.AddDomainDependencies(builder.Services);
        DependencyInjectionModuleInfra.AddInfraDependencies(builder.Services);
        DependencyInjectionModuleService.AddServiceDependencies(builder.Services);
        // Registro do cache de sessão de token Moodle
        builder.Services.AddSingleton<Application.Shared.Session.ISessionTokenCache, Application.Shared.Session.SessionTokenCache>();
        
        var app = builder.Build();

        RunStartupValidation(app.Services);

        var localizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
        app.UseRequestLocalization(localizationOptions);

        app.UseMetricServer(); // Prometheus middleware para expor /metrics
        app.UseHttpMetrics(); // Middleware para coletar métricas HTTP

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
            Authorization = new[] { new HangfireDashboardAuthorizationFilter(allowAnonymous: app.Environment.IsDevelopment()) }
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

        // Metrics endpoint para Prometheus
        app.MapGet("/metrics", async context =>
        {
            context.Response.ContentType = "text/plain; charset=utf-8; version=0.0.4";
            await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(context.Response.Body);
        });

        app.MapControllers();
        app.MapFallbackToFile("/index.html");

        Log.Information("Aplicação iniciada com sucesso.");
        app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Aplicação foi encerrada inesperadamente.");
        }
        finally
        {
            Log.CloseAndFlush();
        }
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

    /// <summary>
    /// Carrega variáveis de ambiente do arquivo .env
    /// Útil para desenvolvimento local quando .env está no root do projeto
    /// </summary>
    private static void LoadEnvironmentVariables()
    {
        var envPath = ".env";
        
        // Tentar em diferentes locais
        if (!File.Exists(envPath))
            envPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())?.FullName ?? "", ".env");
        
        if (!File.Exists(envPath))
            envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");

        if (File.Exists(envPath))
        {
            foreach (var line in File.ReadAllLines(envPath))
            {
                // Pular comentários e linhas vazias
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    
                    // Remover aspas se existirem
                    if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                        (value.StartsWith("'") && value.EndsWith("'")))
                    {
                        value = value[1..^1];
                    }
                    
                    // Apenas set se não está já definido
                    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                    {
                        Environment.SetEnvironmentVariable(key, value);
                    }
                }
            }
        }
    }
}
