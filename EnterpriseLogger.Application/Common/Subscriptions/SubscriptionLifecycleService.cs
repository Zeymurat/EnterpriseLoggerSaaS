using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Domain.Entities;
using EnterpriseLogger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Common.Subscriptions;

public class SubscriptionLifecycleService
{
    private static readonly SubscriptionStatus[] ActiveStatuses =
    [
        SubscriptionStatus.Active,
        SubscriptionStatus.PendingPayment,
        SubscriptionStatus.PastDue
    ];

    private readonly IApplicationDbContext _context;

    public SubscriptionLifecycleService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task DowngradeTenantToFreeAsync(int tenantId, DateTime closedAtUtc, CancellationToken cancellationToken)
    {
        await SupersedeActiveSubscriptionsAsync(tenantId, closedAtUtc, cancellationToken);
        await EnsureFreeSubscriptionAsync(tenantId, closedAtUtc, cancellationToken);
    }

    public async Task CancelActiveSubscriptionsAsync(
        int tenantId,
        string reason,
        DateTime cancelledAtUtc,
        CancellationToken cancellationToken = default)
    {
        var trimmedReason = reason.Trim();
        var activeSubscriptions = await _context.TenantSubscriptions
            .Where(s => s.TenantId == tenantId && ActiveStatuses.Contains(s.Status))
            .ToListAsync(cancellationToken);

        foreach (var subscription in activeSubscriptions)
        {
            subscription.Status = SubscriptionStatus.Cancelled;
            subscription.CancelledAt = cancelledAtUtc;
            subscription.CancellationReason = trimmedReason;
            subscription.EndDate = cancelledAtUtc;
            subscription.AutoRenew = false;
            subscription.GracePeriodEndDate = null;
        }
    }

    public async Task EnsureFreeSubscriptionAsync(
        int tenantId,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var hasActive = await _context.TenantSubscriptions
            .AnyAsync(
                s => s.TenantId == tenantId
                    && ActiveStatuses.Contains(s.Status)
                    && s.EndDate > asOfUtc,
                cancellationToken);

        if (hasActive)
            return;

        var freePackage = await _context.Packages
            .FirstOrDefaultAsync(p => p.Code == PackageCodes.Free && p.IsDefault, cancellationToken);

        if (freePackage is null)
            return;

        _context.TenantSubscriptions.Add(
            SubscriptionHelper.CreateSubscription(
                tenantId,
                freePackage,
                BillingCycle.Monthly,
                isPaid: true,
                autoRenew: false,
                gracePeriodEndDate: null,
                asOfUtc));
    }

    private async Task SupersedeActiveSubscriptionsAsync(
        int tenantId,
        DateTime closedAtUtc,
        CancellationToken cancellationToken)
    {
        var activeSubscriptions = await _context.TenantSubscriptions
            .Where(s => s.TenantId == tenantId && ActiveStatuses.Contains(s.Status))
            .ToListAsync(cancellationToken);

        foreach (var subscription in activeSubscriptions)
            SubscriptionHelper.Supersede(subscription, closedAtUtc);
    }

    public static decimal ResolvePackageAmount(Package package, BillingCycle billingCycle) =>
        billingCycle switch
        {
            BillingCycle.Monthly => package.PriceMonthly,
            BillingCycle.Quarterly => package.PriceQuarterly,
            BillingCycle.SemiAnnual => package.PriceSemiAnnual,
            BillingCycle.Annual => package.PriceAnnual,
            _ => package.PriceMonthly
        };
}
