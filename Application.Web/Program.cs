using Application.Api;
using Application.Domain.Model;
using Application.Infrastructure;
using Application.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Service configuration
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

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
            var issuer = AplicationConstants.JWT_ISSUER;
            var audience = AplicationConstants.JWT_AUDIENCE;
            var signingKey = AplicationConstants.JWT_SIGNING_KEY;

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
        DependencyInjectionModuleInfra.RegisterServices(builder.Services);
        DependencyInjectionModuleService.RegisterServices(builder.Services);
        DependencyInjectionModuleWeb.RegisterServices(builder.Services);

        var app = builder.Build();

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
