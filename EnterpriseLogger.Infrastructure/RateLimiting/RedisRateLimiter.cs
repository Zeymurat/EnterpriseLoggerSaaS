using EnterpriseLogger.Application.Common.Interfaces;
using StackExchange.Redis;

namespace EnterpriseLogger.Infrastructure.RateLimiting;

public class RedisRateLimiter : IRateLimiter
{
    private readonly IConnectionMultiplexer _redis;

    public RedisRateLimiter(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<RateLimitAcquireResult> TryAcquireAsync(
        string scopeKey,
        string bucket,
        RateLimitPolicy policy,
        CancellationToken cancellationToken = default)
    {
        if (policy.RequestsPerWindow <= 0)
            return RateLimitAcquireResult.Allowed();

        var windowIndex = GetCurrentWindowIndex(policy.WindowSeconds);
        var key = $"ratelimit:{bucket}:{scopeKey}:{windowIndex}";
        var db = _redis.GetDatabase();

        var count = await db.StringIncrementAsync(key);
        if (count == 1)
        {
            await db.KeyExpireAsync(
                key,
                TimeSpan.FromSeconds(policy.WindowSeconds * 2));
        }

        if (count > policy.RequestsPerWindow)
            return RateLimitAcquireResult.Denied(GetRetryAfterSeconds(policy.WindowSeconds));

        return RateLimitAcquireResult.Allowed();
    }

    private static long GetCurrentWindowIndex(int windowSeconds) =>
        DateTimeOffset.UtcNow.ToUnixTimeSeconds() / windowSeconds;

    private static int GetRetryAfterSeconds(int windowSeconds)
    {
        var elapsedInWindow = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % windowSeconds);
        return windowSeconds - elapsedInWindow;
    }
}
