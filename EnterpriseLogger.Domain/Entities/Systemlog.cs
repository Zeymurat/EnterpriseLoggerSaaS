namespace EnterpriseLogger.Domain.Entities;

public class SystemLog
{
    public long Id { get; set; }
    public int TenantId { get; set; } // Bu logun hangi müşteriye ait olduğunu çözen kritik bağ!
    public string ApplicationName { get; set; } = string.Empty;
    public string LogLevel { get; set; } = string.Empty; // Info, Warning, Error
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation Property: .NET'e bu logun bir Tenant ile ilişkili olduğunu mimari olarak anlatıyoruz.
    public Tenant Tenant { get; set; } = null!;
}