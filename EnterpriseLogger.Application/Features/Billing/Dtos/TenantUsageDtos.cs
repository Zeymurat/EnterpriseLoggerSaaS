namespace EnterpriseLogger.Application.Features.Billing.Dtos;

public record TenantUsageDto(
    string PackageName,
    int MonthlyLogCount,
    int MonthlyRequestLimit,
    int LogsLastMinute,
    int MaxLogsPerMinute);
