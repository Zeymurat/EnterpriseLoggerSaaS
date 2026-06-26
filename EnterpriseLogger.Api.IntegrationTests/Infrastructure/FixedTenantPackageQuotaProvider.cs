namespace EnterpriseLogger.Api.IntegrationTests.Infrastructure;

using EnterpriseLogger.Application.Common.Interfaces;

public sealed class FixedTenantPackageQuotaProvider : ITenantPackageQuotaProvider
{
    private readonly TenantPackageQuota _quota;

    public FixedTenantPackageQuotaProvider(int maxLogsPerMinute, int monthlyRequestLimit)
    {
        _quota = new TenantPackageQuota(maxLogsPerMinute, monthlyRequestLimit);
    }

    public Task<TenantPackageQuota?> GetAsync(int tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult<TenantPackageQuota?>(_quota);
}
