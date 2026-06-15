using System.Security.Claims;
using EnterpriseLogger.Api.Authorization;
using EnterpriseLogger.Application.Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace EnterpriseLogger.Api.IntegrationTests.Authorization;

public class PermissionAuthorizationHandlerTests
{
    private readonly PermissionAuthorizationHandler _handler = new();

    [Fact]
    public async Task Handle_ApiKeyScheme_SucceedsWithoutPermissionClaim()
    {
        var identity = new ClaimsIdentity(
            [new Claim(AuthClaimTypes.TenantId, "1")],
            AuthSchemeNames.ApiKey);
        var user = new ClaimsPrincipal(identity);
        var requirement = new PermissionRequirement(PermissionCodes.LogsRead);
        var context = new AuthorizationHandlerContext(
            [requirement],
            user,
            resource: null);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_BearerWithPermission_Succeeds()
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(AuthClaimTypes.TenantId, "1"),
                new Claim(AuthClaimTypes.Permission, PermissionCodes.LogsRead)
            ],
            "Bearer");
        var user = new ClaimsPrincipal(identity);
        var requirement = new PermissionRequirement(PermissionCodes.LogsRead);
        var context = new AuthorizationHandlerContext(
            [requirement],
            user,
            resource: null);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_BearerWithoutPermission_DoesNotSucceed()
    {
        var identity = new ClaimsIdentity(
            [new Claim(AuthClaimTypes.TenantId, "1")],
            "Bearer");
        var user = new ClaimsPrincipal(identity);
        var requirement = new PermissionRequirement(PermissionCodes.LogsRead);
        var context = new AuthorizationHandlerContext(
            [requirement],
            user,
            resource: null);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }
}
