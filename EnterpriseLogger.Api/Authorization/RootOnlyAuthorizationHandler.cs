using System.Security.Claims;
using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace EnterpriseLogger.Api.Authorization;

public class RootOnlyAuthorizationHandler : AuthorizationHandler<RootOnlyRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RootOnlyRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        var role = context.User.FindFirst(AuthClaimTypes.Role)?.Value
            ?? context.User.FindFirst(ClaimTypes.Role)?.Value;

        if (Enum.TryParse<TenantUserRole>(role, ignoreCase: true, out var parsed) && parsed == TenantUserRole.Root)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
