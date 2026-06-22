using EnterpriseLogger.Domain.Enums;

namespace EnterpriseLogger.Domain.Entities;

public class TenantSubscription
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int PackageId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? GracePeriodEndDate { get; set; }
    public bool IsPaid { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;
    public bool AutoRenew { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Tenant Tenant { get; set; } = null!;
    public Package Package { get; set; } = null!;
}
