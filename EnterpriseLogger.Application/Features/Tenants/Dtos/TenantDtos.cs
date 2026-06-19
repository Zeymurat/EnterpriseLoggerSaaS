namespace EnterpriseLogger.Application.Features.Tenants.Dtos;

/// <summary>
/// Tenant kaydı: şirket adı + Root sahibi bilgileri.
/// API anahtarı kayıtta dönülmez — giriş sonrası rotate ile üretilir.
/// </summary>
public record CreateTenantRequest(
    string Name,
    string OwnerEmail,
    string OwnerPhone,
    string OwnerPassword);

public record TenantResponseDto(
    int Id,
    string Name,
    string OwnerEmail,
    string OwnerPhone,
    bool IsActive,
    DateTime CreatedAt);

public record RotateApiKeyResponseDto(string ApiKey);
