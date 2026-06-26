using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Infrastructure.Audit;

public class PlatformAuditService : IPlatformAuditService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserProvider _currentUserProvider;

    public PlatformAuditService(
        IApplicationDbContext context,
        ICurrentUserProvider currentUserProvider)
    {
        _context = context;
        _currentUserProvider = currentUserProvider;
    }

    public Task LogAsync(
        string action,
        string entityType,
        int? entityId,
        int? tenantId,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        return WriteAsync(
            _currentUserProvider.UserId,
            _currentUserProvider.Email ?? "platform-admin",
            action,
            entityType,
            entityId,
            tenantId,
            details,
            cancellationToken);
    }

    public Task LogSystemAsync(
        string action,
        string entityType,
        int? entityId,
        int? tenantId,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        return WriteAsync(null, "system", action, entityType, entityId, tenantId, details, cancellationToken);
    }

    private async Task WriteAsync(
        int? platformAdminId,
        string actorEmail,
        string action,
        string entityType,
        int? entityId,
        int? tenantId,
        string? details,
        CancellationToken cancellationToken)
    {
        _context.PlatformAuditLogs.Add(new PlatformAuditLog
        {
            PlatformAdminId = platformAdminId,
            ActorEmail = actorEmail,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            TenantId = tenantId,
            Details = details,
            CreatedAt = DateTime.UtcNow,
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
