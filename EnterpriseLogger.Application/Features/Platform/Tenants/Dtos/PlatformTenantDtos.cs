using EnterpriseLogger.Domain.Enums;

namespace EnterpriseLogger.Application.Features.Platform.Tenants.Dtos;

public record PlatformTenantListFilter(
    string? Name,
    string? RootEmail,
    string? RootPhone,
    string? PackageCode,
    bool? IsActive,
    DateTime? SubscriptionStartFrom,
    DateTime? SubscriptionStartTo);

public record PlatformTenantListItemDto(
    int Id,
    string Name,
    bool IsActive,
    DateTime CreatedAt,
    int UserCount,
    long LogCount,
    string? RootEmail,
    string? RootPhone,
    string? CurrentPackageName,
    SubscriptionStatus? CurrentSubscriptionStatus,
    DateTime? CurrentSubscriptionStartDate);

public record PlatformTenantListResponse(IReadOnlyList<PlatformTenantListItemDto> Tenants);

public record PlatformTenantRootUserDto(int Id, string Email, string Phone);

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
    int MonthlyRequestLimit,
    int? PaymentId,
    string? PaymentReferenceNumber,
    DateTime? CancelledAt,
    string? CancellationReason);

public record PlatformTenantDetailDto(
    int Id,
    string Name,
    bool IsActive,
    DateTime CreatedAt,
    int UserCount,
    long LogCount,
    PlatformTenantRootUserDto? RootUser,
    PlatformSubscriptionDto? CurrentSubscription,
    IReadOnlyList<PlatformSubscriptionDto> SubscriptionHistory,
    bool CanDelete,
    string? DeleteBlockedReason);

public record AssignTenantSubscriptionRequest(
    int PackageId,
    BillingCycle BillingCycle,
    bool AutoRenew,
    bool IsPaid,
    DateTime? GracePeriodEndDate,
    int? PaymentId);

public record AssignTenantSubscriptionResponse(PlatformSubscriptionDto Subscription);

public record LinkSubscriptionPaymentRequest(int PaymentId, int? SubscriptionId);

public record LinkSubscriptionPaymentResponse(PlatformSubscriptionDto Subscription);

public class CancelTenantSubscriptionRequest
{
    public string Reason { get; set; } = string.Empty;
}

public record CancelTenantSubscriptionResponse(PlatformSubscriptionDto Subscription);

public record RemoveTenantSubscriptionResponse(PlatformSubscriptionDto? CurrentSubscription);

public record SetTenantStatusRequest(bool IsActive);

public record ResetTenantRootPasswordResponse(string TemporaryPassword);
