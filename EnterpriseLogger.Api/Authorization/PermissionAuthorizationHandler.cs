using EnterpriseLogger.Application.Common.Constants;
using Microsoft.AspNetCore.Authorization;

namespace EnterpriseLogger.Api.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        if (context.User.Identity.AuthenticationType == AuthSchemeNames.ApiKey)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.HasClaim(c =>
                (c.Type == AuthClaimTypes.Permission || c.Type == "permissions")
                && c.Value == requirement.Permission))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
