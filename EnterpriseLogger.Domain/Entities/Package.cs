namespace EnterpriseLogger.Domain.Entities;

public class Package
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AllowedLogLevels { get; set; } = string.Empty;
    public bool IsMailEnabled { get; set; }
    public bool IsSmsEnabled { get; set; }
    public int MonthlyRequestLimit { get; set; }
    public int MaxLogsPerMinute { get; set; }
    public int StorageRetentionDays { get; set; }
    public decimal PriceMonthly { get; set; }
    public decimal PriceQuarterly { get; set; }
    public decimal PriceSemiAnnual { get; set; }
    public decimal PriceAnnual { get; set; }
    public bool IsDefault { get; set; }
    public bool IsAvailable { get; set; } = true;
    public int SortOrder { get; set; }

    public ICollection<TenantSubscription> Subscriptions { get; set; } = new List<TenantSubscription>();
}
