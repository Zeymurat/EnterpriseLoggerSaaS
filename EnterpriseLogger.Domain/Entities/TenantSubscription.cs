namespace EnterpriseLogger.Domain.Entities;

public class TenantSubscription
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int PackageId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? GracePeriodEndDate { get; set; } // Ödeme gecikirse tanınan ekstra süre (Haftaya ödeyeceğim senaryosu)
    public bool IsPaid { get; set; }

    // İlişkiler
    public Tenant Tenant { get; set; } = null!;
    public Package Package { get; set; } = null!;
}