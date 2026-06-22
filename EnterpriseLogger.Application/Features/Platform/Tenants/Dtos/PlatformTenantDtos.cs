using EnterpriseLogger.Domain.Enums;

namespace EnterpriseLogger.Application.Features.Platform.Tenants.Dtos;

public record PlatformTenantListItemDto(
    int Id,
    string Name,
    bool IsActive,
    DateTime CreatedAt,
    int UserCount,
    long LogCount,
    string? CurrentPackageName,
    SubscriptionStatus? CurrentSubscriptionStatus);

public record PlatformTenantListResponse(IReadOnlyList<PlatformTenantListItemDto> Tenants);

public record PlatformSubscriptionDto(
    int Id,
    int PackageId,
    string PackageCode,
    string PackageName,
    SubscriptionStatus Status,
    BillingCycle BillingCycle,
    DateTime StartDate,
    DateTime EndDate,
    DateTime? GracePeriodEndDate,
    bool IsPaid,
    bool AutoRenew,
    int MaxLogsPerMinute,
    int MonthlyRequestLimit);

public record PlatformTenantDetailDto(
    int Id,
    string Name,
    bool IsActive,
    DateTime CreatedAt,
    int UserCount,
    long LogCount,
    PlatformSubscriptionDto? CurrentSubscription,
    IReadOnlyList<PlatformSubscriptionDto> SubscriptionHistory);

public record AssignTenantSubscriptionRequest(
    int PackageId,
    BillingCycle BillingCycle,
    bool AutoRenew,
    bool IsPaid,
    DateTime? GracePeriodEndDate);

public record AssignTenantSubscriptionResponse(PlatformSubscriptionDto Subscription);
