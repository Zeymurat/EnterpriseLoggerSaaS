using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Common.Subscriptions;
using EnterpriseLogger.Application.Features.Platform.Renewals.Dtos;
using EnterpriseLogger.Domain.Entities;
using EnterpriseLogger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Renewals.Queries;

public class GetPlatformRenewalsQuery
{
    private readonly IApplicationDbContext _context;

    public GetPlatformRenewalsQuery(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PlatformRenewalListResponse>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var renewalHorizon = now.AddDays(7);

        var subscriptions = await _context.TenantSubscriptions
            .AsNoTracking()
            .Include(s => s.Package)
            .Include(s => s.Tenant)
            .Where(s =>
                (s.Status == SubscriptionStatus.PendingPayment && !s.IsPaid && s.EndDate > now)
                || (s.Status == SubscriptionStatus.Active && s.AutoRenew && s.EndDate > now && s.EndDate <= renewalHorizon)
                || (s.Status == SubscriptionStatus.PendingPayment
                    && !s.IsPaid
                    && s.GracePeriodEndDate != null
                    && s.GracePeriodEndDate < now))
            .OrderBy(s => s.EndDate)
            .ToListAsync(cancellationToken);

        var items = subscriptions.Select(s => new PlatformRenewalListItemDto(
            s.TenantId,
            s.Tenant.Name,
            s.Package.Name,
            s.Status,
            s.StartDate,
            s.EndDate,
            s.GracePeriodEndDate,
            s.IsPaid,
            s.AutoRenew,
            Categorize(s, now, renewalHorizon))).ToList();

        return Result<PlatformRenewalListResponse>.Success(new PlatformRenewalListResponse(items));
    }

    private static string Categorize(TenantSubscription subscription, DateTime now, DateTime renewalHorizon)
    {
        if (subscription.Status == SubscriptionStatus.PendingPayment
            && !subscription.IsPaid
            && subscription.GracePeriodEndDate is not null
            && subscription.GracePeriodEndDate < now)
            return "grace_expired";

        if (subscription.Status == SubscriptionStatus.PendingPayment && !subscription.IsPaid)
            return "payment_pending";

        if (subscription.Status == SubscriptionStatus.Active
            && subscription.AutoRenew
            && subscription.EndDate <= renewalHorizon)
            return "upcoming_renewal";

        return "other";
    }
}
