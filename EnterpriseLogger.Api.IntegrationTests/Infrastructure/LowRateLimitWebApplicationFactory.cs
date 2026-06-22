using EnterpriseLogger.Application.Common.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EnterpriseLogger.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Integration tests için düşük log ingestion limiti (InMemory rate limiter).
/// </summary>
public class LowRateLimitWebApplicationFactory : WebApplicationFactory<Program>
{
    public int RequestsPerWindow { get; init; } = 3;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<RateLimitSettings>();
            services.AddSingleton(new RateLimitSettings
            {
                LogIngestRequestsPerWindow = RequestsPerWindow,
                LogIngestWindowSeconds = 60
            });
        });
    }
}
