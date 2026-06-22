using EnterpriseLogger.Domain.Entities;
using EnterpriseLogger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Common.Subscriptions;

public static class SubscriptionHelper
{
  private static readonly SubscriptionStatus[] ActiveStatuses =
  [
      SubscriptionStatus.Active,
      SubscriptionStatus.PendingPayment,
      SubscriptionStatus.PastDue
  ];

  public static async Task<TenantSubscription?> GetActiveSubscriptionAsync(
      IQueryable<TenantSubscription> subscriptions,
      int tenantId,
      CancellationToken cancellationToken = default)
  {
      var now = DateTime.UtcNow;

      return await subscriptions
          .AsNoTracking()
          .Include(s => s.Package)
          .Where(s => s.TenantId == tenantId
              && ActiveStatuses.Contains(s.Status)
              && s.EndDate > now)
          .OrderByDescending(s => s.StartDate)
          .FirstOrDefaultAsync(cancellationToken);
  }

  public static DateTime CalculateEndDate(BillingCycle billingCycle, DateTime startDateUtc) =>
      billingCycle switch
      {
          BillingCycle.Monthly => startDateUtc.AddMonths(1),
          BillingCycle.Quarterly => startDateUtc.AddMonths(3),
          BillingCycle.SemiAnnual => startDateUtc.AddMonths(6),
          BillingCycle.Annual => startDateUtc.AddYears(1),
          _ => startDateUtc.AddMonths(1)
      };

  public static SubscriptionStatus ResolveInitialStatus(bool isPaid) =>
      isPaid ? SubscriptionStatus.Active : SubscriptionStatus.PendingPayment;

  public static void Supersede(TenantSubscription subscription, DateTime closedAtUtc)
  {
      subscription.EndDate = closedAtUtc;
      subscription.Status = SubscriptionStatus.Superseded;
  }

  public static TenantSubscription CreateSubscription(
      int tenantId,
      Package package,
      BillingCycle billingCycle,
      bool isPaid,
      bool autoRenew,
      DateTime? gracePeriodEndDate,
      DateTime startDateUtc)
  {
      return new TenantSubscription
      {
          TenantId = tenantId,
          PackageId = package.Id,
          StartDate = startDateUtc,
          EndDate = CalculateEndDate(billingCycle, startDateUtc),
          GracePeriodEndDate = gracePeriodEndDate,
          IsPaid = isPaid,
          Status = ResolveInitialStatus(isPaid),
          BillingCycle = billingCycle,
          AutoRenew = autoRenew,
          CreatedAt = startDateUtc
      };
  }
}
