using EnterpriseLogger.Application.Common.Authorization;
using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Domain.Enums;
using Xunit;

namespace EnterpriseLogger.Application.Tests.Authorization;

public class PermissionResolverTests
{
    [Fact]
    public void Resolve_Root_ReturnsAllPermissions()
    {
        var permissions = PermissionResolver.Resolve(TenantUserRole.Root);

        Assert.Equal(PermissionCodes.All.Count, permissions.Count);
        Assert.All(PermissionCodes.All, code => Assert.Contains(code, permissions));
    }

    [Fact]
    public void Resolve_Admin_ReturnsAdminSubset()
    {
        var permissions = PermissionResolver.Resolve(TenantUserRole.Admin);

        Assert.Contains(PermissionCodes.LogsRead, permissions);
        Assert.Contains(PermissionCodes.UsersInvite, permissions);
        Assert.DoesNotContain(PermissionCodes.ApiKeysRotate, permissions);
        Assert.DoesNotContain(PermissionCodes.TenantSettingsWrite, permissions);
    }

    [Fact]
    public void Resolve_User_ReturnsOnlyGrantedPermissions()
    {
        var permissions = PermissionResolver.Resolve(
            TenantUserRole.User,
            [PermissionCodes.LogsRead, "invalid:permission"]);

        Assert.Single(permissions);
        Assert.Equal(PermissionCodes.LogsRead, permissions[0]);
    }

    [Fact]
    public void Resolve_User_WithNoGrants_ReturnsEmpty()
    {
        var permissions = PermissionResolver.Resolve(TenantUserRole.User);

        Assert.Empty(permissions);
    }
}
