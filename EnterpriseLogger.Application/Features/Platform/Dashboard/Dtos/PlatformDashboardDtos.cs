namespace EnterpriseLogger.Application.Features.Platform.Dashboard.Dtos;

public record PlatformDashboardDto(
    int TotalTenants,
    int ActiveTenants,
    int PendingPayments,
    int PendingRenewals,
    int UpcomingRenewals,
    int GraceExpiringSoon,
    int HighQuotaTenants,
    IReadOnlyList<PlatformDashboardQuotaItemDto> TopQuotaTenants);

public record PlatformDashboardQuotaItemDto(
    int TenantId,
    string TenantName,
    string PackageName,
    int MonthlyLogCount,
    int MonthlyLimit,
    int UsagePercent);
