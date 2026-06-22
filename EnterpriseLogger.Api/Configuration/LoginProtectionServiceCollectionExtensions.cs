using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Settings;
using EnterpriseLogger.Infrastructure.Security;
using StackExchange.Redis;

namespace EnterpriseLogger.Api.Configuration;

public static class LoginProtectionServiceCollectionExtensions
{
    public static IServiceCollection AddLoginProtection(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        var settings = ResolveSettings(environment);
        services.AddSingleton(settings);

        if (environment.IsEnvironment("Testing"))
        {
            services.AddSingleton<ILoginProtectionService, InMemoryLoginProtectionService>();
            return services;
        }

        services.AddSingleton<ILoginProtectionService, RedisLoginProtectionService>();
        return services;
    }

    private static LoginProtectionSettings ResolveSettings(IHostEnvironment environment)
    {
        if (environment.IsEnvironment("Testing"))
        {
            return new LoginProtectionSettings
            {
                MaxFailedAttemptsPerEmail = ParsePositiveInt(
                    Environment.GetEnvironmentVariable("LOGIN_MAX_FAILED_ATTEMPTS_EMAIL"),
                    1_000),
                MaxFailedAttemptsPerIp = ParsePositiveInt(
                    Environment.GetEnvironmentVariable("LOGIN_MAX_FAILED_ATTEMPTS_IP"),
                    1_000),
                LockoutMinutes = ParsePositiveInt(
                    Environment.GetEnvironmentVariable("LOGIN_LOCKOUT_MINUTES"),
                    15),
                FailCounterWindowSeconds = ParsePositiveInt(
                    Environment.GetEnvironmentVariable("LOGIN_FAIL_WINDOW_SECONDS"),
                    900),
                BackoffStartAfterAttempts = ParsePositiveInt(
                    Environment.GetEnvironmentVariable("LOGIN_BACKOFF_START_AFTER"),
                    3),
                MaxBackoffSeconds = ParsePositiveInt(
                    Environment.GetEnvironmentVariable("LOGIN_MAX_BACKOFF_SECONDS"),
                    8)
            };
        }

        return new LoginProtectionSettings
        {
            MaxFailedAttemptsPerEmail = ParsePositiveInt(
                Environment.GetEnvironmentVariable("LOGIN_MAX_FAILED_ATTEMPTS_EMAIL"),
                10),
            MaxFailedAttemptsPerIp = ParsePositiveInt(
                Environment.GetEnvironmentVariable("LOGIN_MAX_FAILED_ATTEMPTS_IP"),
                30),
            LockoutMinutes = ParsePositiveInt(
                Environment.GetEnvironmentVariable("LOGIN_LOCKOUT_MINUTES"),
                15),
            FailCounterWindowSeconds = ParsePositiveInt(
                Environment.GetEnvironmentVariable("LOGIN_FAIL_WINDOW_SECONDS"),
                900),
            BackoffStartAfterAttempts = ParsePositiveInt(
                Environment.GetEnvironmentVariable("LOGIN_BACKOFF_START_AFTER"),
                3),
            MaxBackoffSeconds = ParsePositiveInt(
                Environment.GetEnvironmentVariable("LOGIN_MAX_BACKOFF_SECONDS"),
                8)
        };
    }

    private static int ParsePositiveInt(string? value, int defaultValue) =>
        int.TryParse(value, out var parsed) && parsed > 0 ? parsed : defaultValue;
}
