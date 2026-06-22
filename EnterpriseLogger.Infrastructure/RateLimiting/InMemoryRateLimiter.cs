using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Settings;

namespace EnterpriseLogger.Infrastructure.RateLimiting;

public class InMemoryRateLimiter : IRateLimiter
{
    private readonly RateLimitSettings _settings;
    private readonly object _sync = new();
    private readonly Dictionary<string, int> _counts = new();

    public InMemoryRateLimiter(RateLimitSettings settings)
    {
        _settings = settings;
    }

    public Task<RateLimitAcquireResult> TryAcquireAsync(
        int tenantId,
        string bucket,
        CancellationToken cancellationToken = default)
    {
        if (_settings.LogIngestRequestsPerWindow <= 0)
            return Task.FromResult(RateLimitAcquireResult.Allowed());

        var windowIndex = GetCurrentWindowIndex();
        var key = $"{bucket}:{tenantId}:{windowIndex}";

        lock (_sync)
        {
            _counts.TryGetValue(key, out var count);
            count++;
            _counts[key] = count;
            PruneStaleWindows(windowIndex);

            if (count > _settings.LogIngestRequestsPerWindow)
                return Task.FromResult(RateLimitAcquireResult.Denied(GetRetryAfterSeconds()));

            return Task.FromResult(RateLimitAcquireResult.Allowed());
        }
    }

    private long GetCurrentWindowIndex() =>
        DateTimeOffset.UtcNow.ToUnixTimeSeconds() / _settings.LogIngestWindowSeconds;

    private int GetRetryAfterSeconds()
    {
        var windowSeconds = _settings.LogIngestWindowSeconds;
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
