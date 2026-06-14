namespace EnterpriseLogger.Application.Features.Tenants.Dtos;

/// <summary>
/// Tenant kaydı: şirket adı + Root sahibi bilgileri. ApiKey sunucu tarafından üretilir.
/// Aynı e-posta farklı tenant'larda kullanılabilir (TenantId + Email unique).
/// </summary>
public record CreateTenantRequest(
    string Name,
    string OwnerEmail,
    string OwnerPhone,
    string OwnerPassword);

public record TenantResponseDto(
    int Id,
    string Name,
    string ApiKey,
    string OwnerEmail,
    string OwnerPhone,
    bool IsActive,
    DateTime CreatedAt);
