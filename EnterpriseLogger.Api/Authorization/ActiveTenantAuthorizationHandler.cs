using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Api.Authorization;

public class ActiveTenantAuthorizationHandler : AuthorizationHandler<ActiveTenantRequirement>
{
    private readonly IApplicationDbContext _dbContext;

    public ActiveTenantAuthorizationHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveTenantRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        var tenantIdValue = context.User.FindFirst(AuthClaimTypes.TenantId)?.Value;
        if (!int.TryParse(tenantIdValue, out var tenantId))
            return;

        var isActive = await _dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => t.IsActive)
            .FirstOrDefaultAsync();

        if (isActive)
            context.Succeed(requirement);
    }
}
