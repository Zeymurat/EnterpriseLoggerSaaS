namespace EnterpriseLogger.Application.Features.Platform.Tenants.Dtos;

public record PlatformTenantListItemDto(
    int Id,
    string Name,
    bool IsActive,
    DateTime CreatedAt,
    int UserCount,
    long LogCount);

public record PlatformTenantListResponse(IReadOnlyList<PlatformTenantListItemDto> Tenants);
