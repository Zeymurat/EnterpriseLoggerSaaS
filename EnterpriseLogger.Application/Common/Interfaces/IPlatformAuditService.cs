namespace EnterpriseLogger.Application.Common.Interfaces;

public interface IPlatformAuditService
{
    Task LogAsync(
        string action,
        string entityType,
        int? entityId,
        int? tenantId,
        string? details = null,
        CancellationToken cancellationToken = default);

    Task LogSystemAsync(
        string action,
        string entityType,
        int? entityId,
        int? tenantId,
        string? details = null,
        CancellationToken cancellationToken = default);
}
