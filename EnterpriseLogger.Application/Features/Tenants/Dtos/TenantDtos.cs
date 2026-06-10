namespace EnterpriseLogger.Application.Features.Tenants.Dtos;

/// <summary>
/// Yalnızca şirket adı gönderilir. ApiKey sunucu tarafından üretilir ve yanıtta döner.
/// </summary>
public record CreateTenantRequest(string Name);

public record TenantResponseDto(int Id, string Name, string ApiKey, bool IsActive, DateTime CreatedAt);
