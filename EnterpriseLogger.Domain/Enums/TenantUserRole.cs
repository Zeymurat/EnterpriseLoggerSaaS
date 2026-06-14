namespace EnterpriseLogger.Domain.Enums;

/// <summary>
/// Tenant içi kullanıcı rolü. Tüm roller yalnızca kendi tenant kapsamında geçerlidir.
/// </summary>
public enum TenantUserRole
{
    Root = 0,
    Admin = 1,
    User = 2
}
