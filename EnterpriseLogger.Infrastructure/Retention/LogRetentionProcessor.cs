using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Subscriptions;
using EnterpriseLogger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Infrastructure.Retention;

public class LogRetentionProcessor
{
    private readonly IApplicationDbContext _context;
    private readonly IPlatformAuditService _auditService;

    public LogRetentionProcessor(
        IApplicationDbContext context,
        IPlatformAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<int> ProcessAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var totalDeleted = 0;

        var tenants = await _context.Tenants
            .AsNoTracking()
            .Select(t => new { t.Id, t.Name })
            .ToListAsync(cancellationToken);

        foreach (var tenant in tenants)
        {
            var subscription = await SubscriptionHelper.GetActiveSubscriptionAsync(
                _context.TenantSubscriptions,
                tenant.Id,
                cancellationToken);

            var retentionDays = subscription?.Package.StorageRetentionDays ?? 30;
            var cutoff = now.AddDays(-retentionDays);

            var deleted = await _context.SystemLogs
                .IgnoreQueryFilters()
                .Where(log => log.TenantId == tenant.Id && log.Timestamp < cutoff)
                .ExecuteDeleteAsync(cancellationToken);

            if (deleted > 0)
            {
                totalDeleted += deleted;
                await _auditService.LogSystemAsync(
                    Application.Common.Constants.PlatformAuditActions.SystemLogRetention,
                    "tenant",
                    tenant.Id,
                    tenant.Id,
                    $"deleted={deleted};retentionDays={retentionDays}",
                    cancellationToken);
            }
        }

        return totalDeleted;
    }
}
