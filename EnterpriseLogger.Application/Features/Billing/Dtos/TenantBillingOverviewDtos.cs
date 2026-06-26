using EnterpriseLogger.Domain.Enums;

namespace EnterpriseLogger.Application.Features.Billing.Dtos;

public record TenantPaymentSummaryDto(
    int Id,
    decimal Amount,
    string Currency,
    PaymentStatus Status,
    string ReferenceNumber,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    DateTime CreatedAt,
    DateTime? ConfirmedAt);

public record TenantBillingOverviewDto(
    bool HasActiveSubscription,
    string? PackageName,
    string? PackageCode,
    SubscriptionStatus? Status,
    BillingCycle? BillingCycle,
    DateTime? StartDate,
    DateTime? EndDate,
    DateTime? GracePeriodEndDate,
    bool IsPaid,
    bool AutoRenew,
    int StorageRetentionDays,
    int MonthlyRequestLimit,
    int MaxLogsPerMinute,
    bool ShowPaymentNotice,
    string? PaymentNoticeMessage,
    IReadOnlyList<TenantPaymentSummaryDto> RecentPayments);
