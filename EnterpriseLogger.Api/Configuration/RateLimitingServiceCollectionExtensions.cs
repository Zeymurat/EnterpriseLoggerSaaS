using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Settings;
using EnterpriseLogger.Infrastructure.RateLimiting;
using StackExchange.Redis;

namespace EnterpriseLogger.Api.Configuration;

public static class RateLimitingServiceCollectionExtensions
{
    public static IServiceCollection AddLogIngestRateLimiting(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        var settings = ResolveSettings(environment);
        services.AddSingleton(settings);

        if (environment.IsEnvironment("Testing"))
        {
            services.AddSingleton<IRateLimiter, InMemoryRateLimiter>();
            return services;
        }

        var redisConnection = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            throw new InvalidOperationException(
                "REDIS_CONNECTION_STRING is not configured. Set it in .env (see .env.example) " +
                "or start Redis via docker-compose.");
        }

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        services.AddSingleton<IRateLimiter, RedisRateLimiter>();

        return services;
    }

    private static RateLimitSettings ResolveSettings(IHostEnvironment environment)
    {
        if (environment.IsEnvironment("Testing"))
        {
            return new RateLimitSettings
            {
                LogIngestRequestsPerWindow = ParsePositiveInt(
                    Environment.GetEnvironmentVariable("LOG_INGEST_RATE_LIMIT_PER_MINUTE"),
                    10_000),
                LogIngestWindowSeconds = ParsePositiveInt(
                    Environment.GetEnvironmentVariable("LOG_INGEST_RATE_LIMIT_WINDOW_SECONDS"),
                    60)
            };
        }

        return new RateLimitSettings
        {
            LogIngestRequestsPerWindow = ParsePositiveInt(
                Environment.GetEnvironmentVariable("LOG_INGEST_RATE_LIMIT_PER_MINUTE"),
                1000),
            LogIngestWindowSeconds = ParsePositiveInt(
                Environment.GetEnvironmentVariable("LOG_INGEST_RATE_LIMIT_WINDOW_SECONDS"),
                60)
        };
    }

    private static int ParsePositiveInt(string? value, int defaultValue) =>
        int.TryParse(value, out var parsed) && parsed > 0 ? parsed : defaultValue;
}
