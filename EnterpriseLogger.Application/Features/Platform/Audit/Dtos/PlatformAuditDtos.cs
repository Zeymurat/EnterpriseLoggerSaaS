namespace EnterpriseLogger.Application.Features.Platform.Audit.Dtos;

public record PlatformAuditLogListItemDto(
    long Id,
    int? PlatformAdminId,
    string ActorEmail,
    string Action,
    string EntityType,
    int? EntityId,
    int? TenantId,
    string? TenantName,
    string? Details,
    DateTime CreatedAt);

public record PlatformAuditLogListResponse(
    IReadOnlyList<PlatformAuditLogListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
