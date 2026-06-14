namespace EnterpriseLogger.Application.Common.Constants;

public static class PermissionCodes
{
    public const string LogsRead = "logs:read";
    public const string LogsWrite = "logs:write";
    public const string UsersRead = "users:read";
    public const string UsersInvite = "users:invite";
    public const string UsersManage = "users:manage";
    public const string TenantSettingsRead = "tenant:settings:read";
    public const string TenantSettingsWrite = "tenant:settings:write";
    public const string ApiKeysRotate = "apikeys:rotate";

    public static readonly IReadOnlyList<string> All =
    [
        LogsRead,
        LogsWrite,
        UsersRead,
        UsersInvite,
        UsersManage,
        TenantSettingsRead,
        TenantSettingsWrite,
        ApiKeysRotate
    ];
}
