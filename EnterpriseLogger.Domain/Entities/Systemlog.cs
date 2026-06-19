namespace EnterpriseLogger.Domain.Entities;

public class SystemLog
{
    public long Id { get; set; }
    public int TenantId { get; set; } // Bu logun hangi müşteriye ait olduğunu çözen kritik bağ!
    public string ApplicationName { get; set; } = string.Empty;
    public string LogLevel { get; set; } = string.Empty; // Info, Warning, Error
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Müşteri uygulamasının gönderdiği istek bağlamı (opsiyonel).</summary>
    public string? HttpMethod { get; set; }
    public string? RequestPath { get; set; }
    public int? StatusCode { get; set; }
    public string? CorrelationId { get; set; }
    /// <summary>Logu tetikleyen son kullanıcı (e-posta, user id vb.) — müşteri uygulaması gönderir.</summary>
    public string? ActorIdentifier { get; set; }
    public string? ExceptionType { get; set; }

    // Navigation Property: .NET'e bu logun bir Tenant ile ilişkili olduğunu mimari olarak anlatıyoruz.
    public Tenant Tenant { get; set; } = null!;
}