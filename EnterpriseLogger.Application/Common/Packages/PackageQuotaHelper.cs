using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Common.Packages;

public static class PackageQuotaHelper
{
    public static DateTime GetCurrentMonthStartUtc()
    {
        var now = DateTime.UtcNow;
        return new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    public static async Task<int> GetMonthlyLogCountAsync(
        IApplicationDbContext context,
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        var monthStart = GetCurrentMonthStartUtc();

        return await context.SystemLogs
            .AsNoTracking()
            .CountAsync(
                log => log.TenantId == tenantId && log.Timestamp >= monthStart,
                cancellationToken);
    }
}
