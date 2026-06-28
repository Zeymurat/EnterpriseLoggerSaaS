using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Subscriptions;
using EnterpriseLogger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Tenants;

internal static class PlatformTenantEnrichmentLoader
{
    internal sealed record Enrichment(
        int LogCount,
        string? RootEmail,
        string? RootPhone,
        string? PackageName,
        SubscriptionStatus? SubscriptionStatus,
        DateTime? SubscriptionStart);

    internal static async Task<IReadOnlyDictionary<int, Enrichment>> LoadAsync(
        IApplicationDbContext context,
        IReadOnlyList<int> tenantIds,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (tenantIds.Count == 0)
            return new Dictionary<int, Enrichment>();

        var logCounts = await context.SystemLogs
            .IgnoreQueryFilters()
            .Where(l => tenantIds.Contains(l.TenantId))
            .GroupBy(l => l.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);

        var rootUsers = await context.Users
            .AsNoTracking()
            .Where(u => tenantIds.Contains(u.TenantId) && u.Role == TenantUserRole.Root)
            .Select(u => new { u.TenantId, u.Email, u.Phone })
            .ToListAsync(cancellationToken);

        var rootByTenant = rootUsers
            .GroupBy(u => u.TenantId)
            .ToDictionary(g => g.Key, g => g.First());

        var subscriptions = await context.TenantSubscriptions
            .AsNoTracking()
            .Where(s => tenantIds.Contains(s.TenantId)
                && SubscriptionHelper.ActiveStatuses.Contains(s.Status)
                && s.EndDate > now)
            .Select(s => new
            {
                s.TenantId,
                s.StartDate,
                s.Status,
                PackageName = s.Package.Name
            })
            .ToListAsync(cancellationToken);

        var subscriptionByTenant = subscriptions
            .GroupBy(s => s.TenantId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(s => s.StartDate).First());

        var result = new Dictionary<int, Enrichment>(tenantIds.Count);
        foreach (var tenantId in tenantIds)
        {
            rootByTenant.TryGetValue(tenantId, out var root);
            subscriptionByTenant.TryGetValue(tenantId, out var subscription);

            result[tenantId] = new Enrichment(
                logCounts.GetValueOrDefault(tenantId),
                root?.Email,
                root?.Phone,
                subscription?.PackageName,
                subscription?.Status,
                subscription?.StartDate);
        }

        return result;
    }
}
