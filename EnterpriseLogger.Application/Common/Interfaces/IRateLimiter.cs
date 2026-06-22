namespace EnterpriseLogger.Application.Common.Interfaces;

public sealed record RateLimitPolicy(int RequestsPerWindow, int WindowSeconds)
{
    public static RateLimitPolicy Disabled => new(0, 60);
}

public interface IRateLimiter
{
    Task<RateLimitAcquireResult> TryAcquireAsync(
        string scopeKey,
        string bucket,
        RateLimitPolicy policy,
        CancellationToken cancellationToken = default);
}

public sealed record RateLimitAcquireResult(bool IsAllowed, int RetryAfterSeconds = 0)
{
    public static RateLimitAcquireResult Allowed() => new(true);

    public static RateLimitAcquireResult Denied(int retryAfterSeconds) =>
        new(false, Math.Max(1, retryAfterSeconds));
}
