using EnterpriseLogger.Application.Common.Settings;
using EnterpriseLogger.Api.IntegrationTests.Infrastructure;
using EnterpriseLogger.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EnterpriseLogger.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Integration tests için düşük paket kotası (InMemory rate limiter).
/// </summary>
public class LowRateLimitWebApplicationFactory : WebApplicationFactory<Program>
{
    public int RequestsPerWindow { get; init; } = 3;

    public int MonthlyRequestLimit { get; init; } = 1_000_000;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<RateLimitSettings>();
            services.AddSingleton(new RateLimitSettings());

            services.RemoveAll<ITenantPackageQuotaProvider>();
            services.AddSingleton<ITenantPackageQuotaProvider>(
                new FixedTenantPackageQuotaProvider(RequestsPerWindow, MonthlyRequestLimit));
        });
    }
}
