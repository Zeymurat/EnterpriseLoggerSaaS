using EnterpriseLogger.Application.Common.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
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
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<RateLimitSettings>();
            services.AddSingleton(new RateLimitSettings
            {
                AuthLoginRequestsPerWindow = AuthLoginRequestsPerWindow,
                AuthLoginWindowSeconds = 60,
                TenantRegisterRequestsPerWindow = TenantRegisterRequestsPerWindow,
                TenantRegisterWindowSeconds = 60
            });

            services.RemoveAll<LoginProtectionSettings>();
            services.AddSingleton(new LoginProtectionSettings
            {
                MaxFailedAttemptsPerEmail = 1_000,
                MaxFailedAttemptsPerIp = 1_000,
                LockoutMinutes = 15,
                FailCounterWindowSeconds = 900,
                BackoffStartAfterAttempts = 1_000,
                MaxBackoffSeconds = 0
            });
        });
    }
}
