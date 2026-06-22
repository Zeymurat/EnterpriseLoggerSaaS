using EnterpriseLogger.Application.Common.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EnterpriseLogger.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Login ve tenant kaydı için düşük IP rate limiti (InMemory).
/// </summary>
public class LowPublicRateLimitWebApplicationFactory : WebApplicationFactory<Program>
{
    public int AuthLoginRequestsPerWindow { get; init; } = 3;

    public int TenantRegisterRequestsPerWindow { get; init; } = 2;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<RateLimitSettings>();
            services.AddSingleton(new RateLimitSettings
            {
                LogIngestRequestsPerWindow = 10_000,
                LogIngestWindowSeconds = 60,
                AuthLoginRequestsPerWindow = AuthLoginRequestsPerWindow,
                AuthLoginWindowSeconds = 60,
                TenantRegisterRequestsPerWindow = TenantRegisterRequestsPerWindow,
                TenantRegisterWindowSeconds = 60
            });
        });
    }
}
