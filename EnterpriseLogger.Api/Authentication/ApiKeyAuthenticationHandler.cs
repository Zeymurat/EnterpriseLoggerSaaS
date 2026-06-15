using System.Security.Claims;
using System.Text.Encodings.Web;
using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnterpriseLogger.Api.Authentication;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IApplicationDbContext _dbContext;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApplicationDbContext dbContext)
        : base(options, logger, encoder)
    {
        _dbContext = dbContext;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(TenantAuthConstants.ApiKeyHeaderName, out var apiKeyValues)
            || string.IsNullOrWhiteSpace(apiKeyValues.FirstOrDefault()))
        {
            return AuthenticateResult.NoResult();
        }

        var apiKey = apiKeyValues.ToString().Trim();

        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ApiKey == apiKey, Context.RequestAborted);

        if (tenant is null)
            return AuthenticateResult.Fail("Geçersiz API Key.");

        var claims = new[]
        {
            new Claim(AuthClaimTypes.TenantId, tenant.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, AuthSchemeNames.ApiKey);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthSchemeNames.ApiKey);

        Context.Items[TenantAuthConstants.TenantIdItemKey] = tenant.Id;

        return AuthenticateResult.Success(ticket);
    }
}
