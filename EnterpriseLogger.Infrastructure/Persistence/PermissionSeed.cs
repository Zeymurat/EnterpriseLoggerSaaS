using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Domain.Entities;

namespace EnterpriseLogger.Infrastructure.Persistence;

internal static class PermissionSeed
{
    public static Permission[] GetPermissions() =>
    [
        new() { Id = 1, Code = PermissionCodes.LogsRead, Description = "Log listeleme" },
        new() { Id = 2, Code = PermissionCodes.LogsWrite, Description = "Log oluşturma" },
        new() { Id = 3, Code = PermissionCodes.UsersRead, Description = "Kullanıcıları listeleme" },
        new() { Id = 4, Code = PermissionCodes.UsersInvite, Description = "Kullanıcı davet etme" },
        new() { Id = 5, Code = PermissionCodes.UsersManage, Description = "Kullanıcı rol ve izin yönetimi" },
        new() { Id = 6, Code = PermissionCodes.TenantSettingsRead, Description = "Tenant ayarlarını görüntüleme" },
        new() { Id = 7, Code = PermissionCodes.TenantSettingsWrite, Description = "Tenant ayarlarını güncelleme" },
        new() { Id = 8, Code = PermissionCodes.ApiKeysRotate, Description = "API anahtarı yenileme" }
    ];
}
