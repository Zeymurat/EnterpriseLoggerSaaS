using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Common.Packages;
using EnterpriseLogger.Application.Common.Subscriptions;
using EnterpriseLogger.Application.Features.Platform.Dashboard.Dtos;
using EnterpriseLogger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Dashboard.Queries;

public class GetPlatformDashboardQuery
{
    private const int QuotaWarningPercent = 80;
    private const int TopQuotaLimit = 5;

    private readonly IApplicationDbContext _context;

    public GetPlatformDashboardQuery(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PlatformDashboardDto>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var renewalHorizon = now.AddDays(7);
        var graceHorizon = now.AddDays(3);

        var totalTenants = await _context.Tenants.CountAsync(cancellationToken);
        var activeTenants = await _context.Tenants.CountAsync(t => t.IsActive, cancellationToken);

        var pendingPayments = await _context.Payments.CountAsync(
            p => p.Status == PaymentStatus.Pending,
            cancellationToken);

        var pendingRenewals = await _context.TenantSubscriptions.CountAsync(
            s => s.Status == SubscriptionStatus.PendingPayment && !s.IsPaid && s.EndDate > now,
            cancellationToken);

        var upcomingRenewals = await _context.TenantSubscriptions.CountAsync(
            s => s.Status == SubscriptionStatus.Active
                && s.AutoRenew
                && s.EndDate > now
                && s.EndDate <= renewalHorizon,
            cancellationToken);

        var graceExpiringSoon = await _context.TenantSubscriptions.CountAsync(
            s => s.Status == SubscriptionStatus.PendingPayment
                && !s.IsPaid
                && s.GracePeriodEndDate != null
                && s.GracePeriodEndDate > now
                && s.GracePeriodEndDate <= graceHorizon,
            cancellationToken);

        var activeSubscriptions = await _context.TenantSubscriptions
            .AsNoTracking()
            .Include(s => s.Package)
            .Include(s => s.Tenant)
            .Where(s => SubscriptionHelper.ActiveStatuses.Contains(s.Status) && s.EndDate > now)
            .ToListAsync(cancellationToken);

        var quotaItems = new List<PlatformDashboardQuotaItemDto>();
        foreach (var subscription in activeSubscriptions)
        {
            var monthlyCount = await PackageQuotaHelper.GetMonthlyLogCountAsync(
                _context,
                subscription.TenantId,
                cancellationToken);

            var limit = subscription.Package.MonthlyRequestLimit;
            if (limit <= 0)
                continue;

            var percent = (int)Math.Round(monthlyCount * 100.0 / limit);
            if (percent < QuotaWarningPercent)
                continue;

            quotaItems.Add(new PlatformDashboardQuotaItemDto(
                subscription.TenantId,
                subscription.Tenant.Name,
                subscription.Package.Name,
                monthlyCount,
                limit,
                percent));
        }

        var topQuota = quotaItems
            .OrderByDescending(item => item.UsagePercent)
            .ThenByDescending(item => item.MonthlyLogCount)
            .Take(TopQuotaLimit)
            .ToList();

        return Result<PlatformDashboardDto>.Success(
            new PlatformDashboardDto(
                totalTenants,
                activeTenants,
                pendingPayments,
                pendingRenewals,
                upcomingRenewals,
                graceExpiringSoon,
                quotaItems.Count,
                topQuota));
    }
}
