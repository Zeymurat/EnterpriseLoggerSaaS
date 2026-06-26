namespace EnterpriseLogger.Domain.Entities;

public class PlatformAuditLog
{
    public long Id { get; set; }
    public int? PlatformAdminId { get; set; }
    public string ActorEmail { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public int? TenantId { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PlatformAdmin? PlatformAdmin { get; set; }
    public Tenant? Tenant { get; set; }
}
