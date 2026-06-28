using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Domain.Enums;

namespace EnterpriseLogger.Application.Common.Authorization;

public static class PermissionResolver
{
    private static readonly IReadOnlyList<string> AdminPermissions =
    [
        PermissionCodes.LogsRead,
        PermissionCodes.LogsWrite,
        PermissionCodes.UsersRead,
        PermissionCodes.UsersInvite,
        PermissionCodes.UsersManage
    ];

    public static IReadOnlyList<string> Resolve(TenantUserRole role, IEnumerable<string>? grantedPermissionCodes = null) =>
        role switch
        {
            TenantUserRole.Root => PermissionCodes.All,
            TenantUserRole.Admin => AdminPermissions,
            TenantUserRole.User => grantedPermissionCodes?
                .Where(PermissionCodes.All.Contains)
                .Distinct(StringComparer.Ordinal)
                .ToList() ?? [],
            _ => []
        };
}
