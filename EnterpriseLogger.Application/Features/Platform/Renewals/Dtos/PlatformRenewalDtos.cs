using EnterpriseLogger.Domain.Enums;

namespace EnterpriseLogger.Application.Features.Platform.Renewals.Dtos;

public record PlatformRenewalListItemDto(
    int TenantId,
    string TenantName,
    string PackageName,
    SubscriptionStatus Status,
    DateTime StartDate,
    DateTime EndDate,
    DateTime? GracePeriodEndDate,
    bool IsPaid,
    bool AutoRenew,
    string Category);

public record PlatformRenewalListResponse(IReadOnlyList<PlatformRenewalListItemDto> Items);
