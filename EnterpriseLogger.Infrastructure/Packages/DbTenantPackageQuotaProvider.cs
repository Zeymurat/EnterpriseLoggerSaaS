using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Infrastructure.Packages;

public class DbTenantPackageQuotaProvider : ITenantPackageQuotaProvider
{
    private readonly IApplicationDbContext _context;
    private readonly Dictionary<int, TenantPackageQuota?> _requestCache = new();

    public DbTenantPackageQuotaProvider(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TenantPackageQuota?> GetAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        if (_requestCache.TryGetValue(tenantId, out var cached))
            return cached;

        var subscription = await SubscriptionHelper.GetActiveSubscriptionAsync(
            _context.TenantSubscriptions.AsNoTracking(),
            tenantId,
            cancellationToken);

        var quota = subscription is null
            ? null
            : new TenantPackageQuota(
                subscription.Package.MaxLogsPerMinute,
                subscription.Package.MonthlyRequestLimit);

        _requestCache[tenantId] = quota;
        return quota;
    }
}
