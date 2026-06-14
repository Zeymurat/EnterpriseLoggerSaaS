using EnterpriseLogger.Domain.Enums;

namespace EnterpriseLogger.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public TenantUserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}
