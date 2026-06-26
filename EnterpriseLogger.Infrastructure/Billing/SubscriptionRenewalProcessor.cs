using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Settings;
using EnterpriseLogger.Application.Common.Subscriptions;
using EnterpriseLogger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Infrastructure.Billing;

public class SubscriptionRenewalProcessor
{
    private readonly IApplicationDbContext _context;
    private readonly SubscriptionLifecycleService _lifecycle;
    private readonly BillingSettings _settings;
    private readonly IPlatformAuditService _auditService;

    public SubscriptionRenewalProcessor(
        IApplicationDbContext context,
        SubscriptionLifecycleService lifecycle,
        BillingSettings settings,
        IPlatformAuditService auditService)
    {
        _context = context;
        _lifecycle = lifecycle;
        _settings = settings;
        _auditService = auditService;
    }

    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        await ProcessGraceExpiredAsync(now, cancellationToken);
        await ProcessEndedWithoutAutoRenewAsync(now, cancellationToken);
        await ProcessEndedWithAutoRenewAsync(now, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessGraceExpiredAsync(DateTime now, CancellationToken cancellationToken)
    {
        var expired = await _context.TenantSubscriptions
            .Where(s => s.Status == SubscriptionStatus.PendingPayment
                && !s.IsPaid
                && s.GracePeriodEndDate != null
                && s.GracePeriodEndDate < now)
            .Select(s => s.TenantId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var tenantId in expired)
            await _lifecycle.DowngradeTenantToFreeAsync(tenantId, now, cancellationToken);
    }

    private async Task ProcessEndedWithoutAutoRenewAsync(DateTime now, CancellationToken cancellationToken)
    {
        var tenantIds = await _context.TenantSubscriptions
            .Where(s => s.Status == SubscriptionStatus.Active
                && !s.AutoRenew
                && s.EndDate <= now)
            .Select(s => s.TenantId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var tenantId in tenantIds)
            await _lifecycle.DowngradeTenantToFreeAsync(tenantId, now, cancellationToken);
    }

    private async Task ProcessEndedWithAutoRenewAsync(DateTime now, CancellationToken cancellationToken)
    {
        var ended = await _context.TenantSubscriptions
            .Include(s => s.Package)
            .Where(s => s.Status == SubscriptionStatus.Active
                && s.AutoRenew
                && s.EndDate <= now)
            .ToListAsync(cancellationToken);

        foreach (var current in ended)
        {
            var hasRenewal = await _context.TenantSubscriptions.AnyAsync(
                s => s.TenantId == current.TenantId
                    && s.StartDate >= current.EndDate
                    && s.Status == SubscriptionStatus.PendingPayment,
                cancellationToken);

            if (hasRenewal)
            {
                SubscriptionHelper.Supersede(current, now);
                continue;
            }

            SubscriptionHelper.Supersede(current, now);

            var renewal = SubscriptionHelper.CreateSubscription(
                current.TenantId,
                current.Package,
                current.BillingCycle,
                isPaid: false,
                autoRenew: true,
                gracePeriodEndDate: now.AddDays(_settings.PaymentGraceDays),
                now);

            renewal.Status = SubscriptionStatus.PendingPayment;
            _context.TenantSubscriptions.Add(renewal);

            await _auditService.LogSystemAsync(
                PlatformAuditActions.SystemSubscriptionRenewal,
                "subscription",
                null,
                current.TenantId,
                $"packageId={current.PackageId}",
                cancellationToken);
        }
    }
}
