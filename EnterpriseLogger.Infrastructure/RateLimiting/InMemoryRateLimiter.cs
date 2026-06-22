using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Settings;

namespace EnterpriseLogger.Infrastructure.RateLimiting;

public class InMemoryRateLimiter : IRateLimiter
{
    private readonly object _sync = new();
    private readonly Dictionary<string, int> _counts = new();

    public Task<RateLimitAcquireResult> TryAcquireAsync(
        string scopeKey,
        string bucket,
        RateLimitPolicy policy,
        CancellationToken cancellationToken = default)
    {
        if (policy.RequestsPerWindow <= 0)
            return Task.FromResult(RateLimitAcquireResult.Allowed());

        var windowIndex = GetCurrentWindowIndex(policy.WindowSeconds);
        var key = $"{bucket}:{scopeKey}:{windowIndex}";

        lock (_sync)
        {
            _counts.TryGetValue(key, out var count);
            count++;
            _counts[key] = count;
            PruneStaleWindows(windowIndex);

            if (count > policy.RequestsPerWindow)
                return Task.FromResult(RateLimitAcquireResult.Denied(GetRetryAfterSeconds(policy.WindowSeconds)));

            return Task.FromResult(RateLimitAcquireResult.Allowed());
        }
    }

    private static long GetCurrentWindowIndex(int windowSeconds) =>
        DateTimeOffset.UtcNow.ToUnixTimeSeconds() / windowSeconds;

    private static int GetRetryAfterSeconds(int windowSeconds)
    {
        var elapsedInWindow = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % windowSeconds);
        return windowSeconds - elapsedInWindow;
    }

    private void PruneStaleWindows(long currentWindowIndex)
    {
        if (_counts.Count < 512)
            return;

        var staleKeys = _counts.Keys
            .Where(key => !key.EndsWith($":{currentWindowIndex}", StringComparison.Ordinal))
            .Take(256)
            .ToList();

        foreach (var staleKey in staleKeys)
            _counts.Remove(staleKey);
    }
}
