using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Settings;

namespace EnterpriseLogger.Infrastructure.Security;

public class InMemoryLoginProtectionService : ILoginProtectionService
{
    private readonly LoginProtectionSettings _settings;
    private readonly object _sync = new();
    private readonly Dictionary<string, CounterEntry> _failCounters = new();
    private readonly Dictionary<string, DateTimeOffset> _locks = new();

    public InMemoryLoginProtectionService(LoginProtectionSettings settings)
    {
        _settings = settings;
    }

    public Task<LoginProtectionStatus> CheckAsync(
        string normalizedEmail,
        string clientIp,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            PruneExpired();

            if (TryGetLockRemainingSeconds(EmailLockKey(normalizedEmail), out var emailLock))
                return Task.FromResult(LoginProtectionStatus.Locked(emailLock));

            if (TryGetLockRemainingSeconds(IpLockKey(clientIp), out var ipLock))
                return Task.FromResult(LoginProtectionStatus.Locked(ipLock));

            return Task.FromResult(LoginProtectionStatus.Allowed());
        }
    }

    public async Task RecordFailureAsync(
        string normalizedEmail,
        string clientIp,
        CancellationToken cancellationToken = default)
    {
        long emailFails;
        long ipFails;

        lock (_sync)
        {
            PruneExpired();
            emailFails = IncrementCounter(EmailFailKey(normalizedEmail));
            ipFails = IncrementCounter(IpFailKey(clientIp));

            if (emailFails >= _settings.MaxFailedAttemptsPerEmail)
                SetLock(EmailLockKey(normalizedEmail));

            if (ipFails >= _settings.MaxFailedAttemptsPerIp)
                SetLock(IpLockKey(clientIp));
        }

        await ApplyBackoffAsync(Math.Max(emailFails, ipFails), cancellationToken);
    }

    public Task RecordSuccessAsync(
        string normalizedEmail,
        string clientIp,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _failCounters.Remove(EmailFailKey(normalizedEmail));
            _failCounters.Remove(IpFailKey(clientIp));
            _locks.Remove(EmailLockKey(normalizedEmail));
            _locks.Remove(IpLockKey(clientIp));
        }

        return Task.CompletedTask;
    }

    private long IncrementCounter(string key)
    {
        var now = DateTimeOffset.UtcNow;
        if (!_failCounters.TryGetValue(key, out var entry) || entry.ExpiresAt <= now)
        {
            _failCounters[key] = new CounterEntry(1, now.AddSeconds(_settings.FailCounterWindowSeconds));
            return 1;
        }

        var nextCount = entry.Count + 1;
        _failCounters[key] = entry with { Count = nextCount };
        return nextCount;
    }

    private void SetLock(string key)
    {
        _locks[key] = DateTimeOffset.UtcNow.AddMinutes(_settings.LockoutMinutes);
    }

    private bool TryGetLockRemainingSeconds(string key, out int seconds)
    {
        seconds = 0;
        if (!_locks.TryGetValue(key, out var expiresAt))
            return false;

        var remaining = expiresAt - DateTimeOffset.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            _locks.Remove(key);
            return false;
        }

        seconds = (int)Math.Ceiling(remaining.TotalSeconds);
        return true;
    }

    private void PruneExpired()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var key in _failCounters.Keys.Where(key => _failCounters[key].ExpiresAt <= now).ToList())
            _failCounters.Remove(key);

        foreach (var key in _locks.Keys.Where(key => _locks[key] <= now).ToList())
            _locks.Remove(key);
    }

    private async Task ApplyBackoffAsync(long failureCount, CancellationToken cancellationToken)
    {
        if (failureCount < _settings.BackoffStartAfterAttempts)
            return;

        var exponent = failureCount - _settings.BackoffStartAfterAttempts;
        var delaySeconds = (int)Math.Min(Math.Pow(2, exponent), _settings.MaxBackoffSeconds);
        if (delaySeconds > 0)
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
    }

    private static string EmailFailKey(string email) => $"login-fail:email:{email}";

    private static string IpFailKey(string ip) => $"login-fail:ip:{ip}";

    private static string EmailLockKey(string email) => $"login-lock:email:{email}";

    private static string IpLockKey(string ip) => $"login-lock:ip:{ip}";

    private sealed record CounterEntry(long Count, DateTimeOffset ExpiresAt);
}
