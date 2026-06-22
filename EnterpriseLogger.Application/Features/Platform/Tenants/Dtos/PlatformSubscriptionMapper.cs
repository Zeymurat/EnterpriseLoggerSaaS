using EnterpriseLogger.Application.Features.Platform.Tenants.Dtos;
using EnterpriseLogger.Domain.Entities;
using EnterpriseLogger.Domain.Enums;

namespace EnterpriseLogger.Application.Features.Platform.Tenants.Dtos;

public static class PlatformSubscriptionMapper
{
    public static PlatformSubscriptionDto ToDto(TenantSubscription subscription) =>
        new(
            subscription.Id,
            subscription.PackageId,
            subscription.Package.Code,
            subscription.Package.Name,
            subscription.Status,
            subscription.BillingCycle,
            subscription.StartDate,
            subscription.EndDate,
            subscription.GracePeriodEndDate,
            subscription.IsPaid,
            subscription.AutoRenew,
            subscription.Package.MaxLogsPerMinute,
            subscription.Package.MonthlyRequestLimit);
}
