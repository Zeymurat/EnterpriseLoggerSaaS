using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EnterpriseLogger.Api.Authentication;
using EnterpriseLogger.Api.Authorization;
using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Settings;
using EnterpriseLogger.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace EnterpriseLogger.Api.Configuration;

public static class AuthenticationServiceCollectionExtensions
{
    public const string DualAuthScheme = "DualAuth";

    public static IServiceCollection AddDualAuthentication(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        var settings = ResolveSettings(environment);
        services.AddSingleton(settings);
        services.AddSingleton(new SessionSettings { MaxSessionHours = settings.MaxSessionHours });

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = DualAuthScheme;
            options.DefaultChallengeScheme = DualAuthScheme;
        })
        .AddPolicyScheme(DualAuthScheme, "ApiKey or Bearer JWT", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                if (context.Request.Headers.ContainsKey(TenantAuthConstants.ApiKeyHeaderName))
                    return AuthSchemeNames.ApiKey;

                return JwtBearerDefaults.AuthenticationScheme;
            };
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
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
        })
        .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
            AuthSchemeNames.ApiKey,
            _ => { });

        services.AddAuthorization(options =>
        {
            AddJwtPermissionPolicy(options, AuthPolicies.LogsRead, PermissionCodes.LogsRead, DualAuthScheme);
            AddJwtPermissionPolicy(options, AuthPolicies.LogsWrite, PermissionCodes.LogsWrite, DualAuthScheme);

            AddJwtPermissionPolicy(options, AuthPolicies.UsersRead, PermissionCodes.UsersRead, JwtBearerDefaults.AuthenticationScheme);
            AddJwtPermissionPolicy(options, AuthPolicies.UsersInvite, PermissionCodes.UsersInvite, JwtBearerDefaults.AuthenticationScheme);
            AddJwtPermissionPolicy(options, AuthPolicies.UsersManage, PermissionCodes.UsersManage, JwtBearerDefaults.AuthenticationScheme);
            AddJwtPermissionPolicy(options, AuthPolicies.ApiKeysRotate, PermissionCodes.ApiKeysRotate, JwtBearerDefaults.AuthenticationScheme);

            options.AddPolicy(AuthPolicies.TenantRootOnly, policy =>
            {
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new RootOnlyRequirement(), new ActiveTenantRequirement());
            });

            options.AddPolicy(AuthPolicies.PlatformAdminOnly, policy =>
            {
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new PlatformAdminRequirement());
            });
        });

        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemDetailsAuthorizationMiddlewareResultHandler>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, ActiveTenantAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, RootOnlyAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, PlatformAdminAuthorizationHandler>();

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
                    60),
                MaxSessionHours = ParseMaxSessionHours(
                    Environment.GetEnvironmentVariable("JWT_MAX_SESSION_HOURS"),
                    8)
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
                60),
            MaxSessionHours = ParseMaxSessionHours(
                Environment.GetEnvironmentVariable("JWT_MAX_SESSION_HOURS"),
                8)
        };
    }

    private static int ParseExpiryMinutes(string? value, int defaultValue) =>
        int.TryParse(value, out var minutes) && minutes > 0 ? minutes : defaultValue;

    private static int ParseMaxSessionHours(string? value, int defaultValue) =>
        int.TryParse(value, out var hours) && hours > 0 ? hours : defaultValue;

    private static void AddJwtPermissionPolicy(
        AuthorizationOptions options,
        string policyName,
        string permission,
        string authenticationScheme)
    {
        options.AddPolicy(policyName, policy =>
        {
            policy.AddAuthenticationSchemes(authenticationScheme);
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(
                new PermissionRequirement(permission),
                new ActiveTenantRequirement());
        });
    }
}
