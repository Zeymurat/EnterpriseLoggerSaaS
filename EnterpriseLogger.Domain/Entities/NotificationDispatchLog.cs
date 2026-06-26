namespace EnterpriseLogger.Domain.Entities;

public class NotificationDispatchLog
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string ReferenceKey { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public Tenant Tenant { get; set; } = null!;
}
