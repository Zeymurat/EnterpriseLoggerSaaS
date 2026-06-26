using EnterpriseLogger.Domain.Enums;

namespace EnterpriseLogger.Domain.Entities;

public class Payment
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int PackageId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string Method { get; set; } = "Havale";
    public string ReferenceNumber { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
    public int? ConfirmedByPlatformAdminId { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public Package Package { get; set; } = null!;
    public TenantSubscription? Subscription { get; set; }
}
