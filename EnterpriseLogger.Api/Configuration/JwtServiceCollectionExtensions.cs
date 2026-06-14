using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace EnterpriseLogger.Api.Configuration;

public static class JwtServiceCollectionExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        var settings = ResolveSettings(environment);
        services.AddSingleton(settings);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
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
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret)),
                ClockSkew = TimeSpan.FromMinutes(1),
                NameClaimType = ClaimTypes.NameIdentifier,
                RoleClaimType = AuthClaimTypes.Role
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    if (context.Principal?.Identity is ClaimsIdentity identity
                        && identity.FindFirst(AuthClaimTypes.TenantId)?.Value is { } tenantIdValue
                        && int.TryParse(tenantIdValue, out var tenantId))
                    {
                        context.HttpContext.Items[TenantAuthConstants.TenantIdItemKey] = tenantId;
                    }

                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    private static JwtSettings ResolveSettings(IHostEnvironment environment)
    {
        if (environment.IsEnvironment("Testing"))
        {
            return new JwtSettings
            {
                Secret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? JwtSettings.TestingSecret,
                Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "EnterpriseLogger",
                Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "EnterpriseLogger.Api",
                AccessTokenExpiryMinutes = ParseExpiryMinutes(
                    Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRY_MINUTES"),
                    60)
            };
        }

        var secret = Environment.GetEnvironmentVariable("JWT_SECRET");
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT_SECRET is not configured or too short. Set JWT_SECRET in .env (min 32 characters).");
        }

        return new JwtSettings
        {
            Secret = secret,
            Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "EnterpriseLogger",
            Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "EnterpriseLogger.Api",
            AccessTokenExpiryMinutes = ParseExpiryMinutes(
                Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRY_MINUTES"),
                60)
        };
    }

    private static int ParseExpiryMinutes(string? value, int defaultValue) =>
        int.TryParse(value, out var minutes) && minutes > 0 ? minutes : defaultValue;
}
