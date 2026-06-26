namespace EnterpriseLogger.Application.Common.Interfaces;

public sealed record TenantPackageQuota(int MaxLogsPerMinute, int MonthlyRequestLimit);

public interface ITenantPackageQuotaProvider
{
    Task<TenantPackageQuota?> GetAsync(int tenantId, CancellationToken cancellationToken = default);
}
