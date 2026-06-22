using EnterpriseLogger.Application.Common.Constants;
using Microsoft.AspNetCore.Authorization;

namespace EnterpriseLogger.Api.Authorization;

public class PlatformAdminAuthorizationHandler : AuthorizationHandler<PlatformAdminRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PlatformAdminRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        var isPlatformAdmin = context.User.FindFirst(AuthClaimTypes.IsPlatformAdmin)?.Value;
        if (string.Equals(isPlatformAdmin, "true", StringComparison.OrdinalIgnoreCase))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
