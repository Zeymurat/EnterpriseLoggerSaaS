namespace EnterpriseLogger.Application.Common.Interfaces;

public interface IRateLimiter
{
    Task<RateLimitAcquireResult> TryAcquireAsync(
        int tenantId,
        string bucket,
        CancellationToken cancellationToken = default);
}

public sealed record RateLimitAcquireResult(bool IsAllowed, int RetryAfterSeconds = 0)
{
    public static RateLimitAcquireResult Allowed() => new(true);

    public static RateLimitAcquireResult Denied(int retryAfterSeconds) =>
        new(false, Math.Max(1, retryAfterSeconds));
}
