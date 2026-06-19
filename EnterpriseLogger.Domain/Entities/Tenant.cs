namespace EnterpriseLogger.Domain.Entities;

public class Tenant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ApiKeyHash { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Property: .NET'e bir Tenant'ın birden fazla abonelik geçmişi (Subscription) 
    // ve birden fazla Log kaydı olabileceğini mimari olarak anlatıyoruz.
    public ICollection<TenantSubscription> Subscriptions { get; set; } = new List<TenantSubscription>();
    public ICollection<SystemLog> Logs { get; set; } = new List<SystemLog>();
    public ICollection<User> Users { get; set; } = new List<User>();
}