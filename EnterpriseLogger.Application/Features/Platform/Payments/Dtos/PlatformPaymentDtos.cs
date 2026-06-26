namespace EnterpriseLogger.Application.Features.Platform.Payments.Dtos;

using EnterpriseLogger.Domain.Enums;

public record PlatformPaymentDto(
    int Id,
    int TenantId,
    string TenantName,
    int PackageId,
    string PackageName,
    decimal Amount,
    string Currency,
    string Method,
    string ReferenceNumber,
    PaymentStatus Status,
    BillingCycle BillingCycle,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    string Notes,
    DateTime CreatedAt,
    DateTime? ConfirmedAt,
    int? LinkedSubscriptionId);

public record PlatformPaymentListResponse(IReadOnlyList<PlatformPaymentDto> Payments);

public record RecordPlatformPaymentRequest(
    int TenantId,
    int PackageId,
    BillingCycle BillingCycle,
    decimal Amount,
    string ReferenceNumber,
    DateTime PeriodStart,
    string? Notes);

public record ConfirmPlatformPaymentRequest(string? Notes);

public record RejectPlatformPaymentRequest(string? Notes);

public record UpdatePlatformPaymentRequest(
    int PackageId,
    BillingCycle BillingCycle,
    decimal Amount,
    string ReferenceNumber,
    DateTime PeriodStart,
    string? Notes);
