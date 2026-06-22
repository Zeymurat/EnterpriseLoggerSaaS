using EnterpriseLogger.Application.Common.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EnterpriseLogger.Api.IntegrationTests.Infrastructure;

public class LowLoginProtectionWebApplicationFactory : WebApplicationFactory<Program>
{
    public int MaxFailedAttemptsPerEmail { get; init; } = 3;

    public int MaxFailedAttemptsPerIp { get; init; } = 100;

    public int MaxBackoffSeconds { get; init; } = 0;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<LoginProtectionSettings>();
            services.AddSingleton(new LoginProtectionSettings
            {
                MaxFailedAttemptsPerEmail = MaxFailedAttemptsPerEmail,
                MaxFailedAttemptsPerIp = MaxFailedAttemptsPerIp,
                LockoutMinutes = 15,
                FailCounterWindowSeconds = 900,
                BackoffStartAfterAttempts = 3,
                MaxBackoffSeconds = MaxBackoffSeconds
            });
        });
    }
}
