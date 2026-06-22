using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Settings;
using StackExchange.Redis;

namespace EnterpriseLogger.Infrastructure.RateLimiting;

public class RedisRateLimiter : IRateLimiter
{
    private readonly IConnectionMultiplexer _redis;
    private readonly RateLimitSettings _settings;

    public RedisRateLimiter(IConnectionMultiplexer redis, RateLimitSettings settings)
    {
        _redis = redis;
        _settings = settings;
    }

    public async Task<RateLimitAcquireResult> TryAcquireAsync(
        int tenantId,
        string bucket,
        CancellationToken cancellationToken = default)
    {
        if (_settings.LogIngestRequestsPerWindow <= 0)
            return RateLimitAcquireResult.Allowed();

        var windowIndex = GetCurrentWindowIndex();
        var key = $"ratelimit:{bucket}:{tenantId}:{windowIndex}";
        var db = _redis.GetDatabase();

        var count = await db.StringIncrementAsync(key);
        if (count == 1)
        {
            await db.KeyExpireAsync(
                key,
                TimeSpan.FromSeconds(_settings.LogIngestWindowSeconds * 2));
        }

        if (count > _settings.LogIngestRequestsPerWindow)
            return RateLimitAcquireResult.Denied(GetRetryAfterSeconds());

        return RateLimitAcquireResult.Allowed();
    }

    private long GetCurrentWindowIndex() =>
        DateTimeOffset.UtcNow.ToUnixTimeSeconds() / _settings.LogIngestWindowSeconds;

    private static int GetRetryAfterSeconds(int windowSeconds)
    {
        var elapsedInWindow = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % windowSeconds);
        return windowSeconds - elapsedInWindow;
    }

    private int GetRetryAfterSeconds() => GetRetryAfterSeconds(_settings.LogIngestWindowSeconds);
}
