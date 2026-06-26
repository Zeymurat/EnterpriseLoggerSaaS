using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Common.Subscriptions;
using EnterpriseLogger.Application.Features.Billing.Dtos;
using EnterpriseLogger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Billing.Queries;

public class GetTenantBillingOverviewQuery
{
    private const int RecentPaymentLimit = 10;

    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantProvider _tenantProvider;

    public GetTenantBillingOverviewQuery(
        IApplicationDbContext context,
        ICurrentTenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<TenantBillingOverviewDto>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        if (_tenantProvider.TenantId is not int tenantId)
            return Result<TenantBillingOverviewDto>.Forbidden("Tenant bağlamı gerekli.");

        var now = DateTime.UtcNow;

        var subscription = await SubscriptionHelper.GetActiveSubscriptionAsync(
            _context.TenantSubscriptions,
            tenantId,
            cancellationToken);

        var payments = await _context.Payments
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(RecentPaymentLimit)
            .Select(p => new TenantPaymentSummaryDto(
                p.Id,
                p.Amount,
                p.Currency,
                p.Status,
                p.ReferenceNumber,
                p.PeriodStart,
                p.PeriodEnd,
                p.CreatedAt,
                p.ConfirmedAt))
            .ToListAsync(cancellationToken);

        var showPaymentNotice = subscription is not null
            && subscription.Status == SubscriptionStatus.PendingPayment
            && !subscription.IsPaid
            && subscription.EndDate > now;

        return Result<TenantBillingOverviewDto>.Success(
            new TenantBillingOverviewDto(
                subscription is not null,
                subscription?.Package.Name,
                subscription?.Package.Code,
                subscription?.Status,
                subscription?.BillingCycle,
                subscription?.StartDate,
                subscription?.EndDate,
                subscription?.GracePeriodEndDate,
                subscription?.IsPaid ?? false,
                subscription?.AutoRenew ?? false,
                subscription?.Package.StorageRetentionDays ?? 0,
                subscription?.Package.MonthlyRequestLimit ?? 0,
                subscription?.Package.MaxLogsPerMinute ?? 0,
                showPaymentNotice,
                showPaymentNotice
                    ? "Paketiniz yenilendi. Havale/EFT ödemenizi tamamlayın; aksi halde süre sonunda Free pakete düşersiniz."
                    : null,
                payments));
    }
}
