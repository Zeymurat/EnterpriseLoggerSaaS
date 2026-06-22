namespace EnterpriseLogger.Domain.Entities;

/// <summary>
/// Platform (host) yöneticisi. Tenant kapsamı dışındadır; Users tablosundan ayrı tutulur.
/// </summary>
public class PlatformAdmin
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}
