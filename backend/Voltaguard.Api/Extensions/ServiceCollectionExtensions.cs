using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Voltaguard.BLL.Security;
using Voltaguard.Common.Constants;

namespace Voltaguard.Api.Extensions;

/// <summary>Groups the API-layer service registration so Program.cs stays short.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        services.Configure<JwtSettings>(jwtSection);
        var settings = jwtSection.Get<JwtSettings>()!;

        // Identity registers a cookie scheme as the default; override it so the API
        // authenticates and challenges with JWT bearer instead of redirecting to a
        // login page.
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = settings.Issuer,
                    ValidAudience = settings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey))
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(RoleConstants.Admin, policy => policy.RequireRole(RoleConstants.Admin));
        });

        return services;
    }

    public static IServiceCollection AddFrontendCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
            {
                policy.WithOrigins("http://localhost:5173")
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        return services;
    }
}
