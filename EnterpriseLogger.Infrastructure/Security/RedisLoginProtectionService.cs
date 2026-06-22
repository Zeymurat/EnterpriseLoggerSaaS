using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Settings;
using StackExchange.Redis;

namespace EnterpriseLogger.Infrastructure.Security;

public class RedisLoginProtectionService : ILoginProtectionService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly LoginProtectionSettings _settings;

    public RedisLoginProtectionService(IConnectionMultiplexer redis, LoginProtectionSettings settings)
    {
        _redis = redis;
        _settings = settings;
    }

    public async Task<LoginProtectionStatus> CheckAsync(
        string normalizedEmail,
        string clientIp,
        CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();

        var emailLockSeconds = await GetLockRemainingSecondsAsync(db, EmailLockKey(normalizedEmail));
        if (emailLockSeconds > 0)
            return LoginProtectionStatus.Locked(emailLockSeconds);

        var ipLockSeconds = await GetLockRemainingSecondsAsync(db, IpLockKey(clientIp));
        if (ipLockSeconds > 0)
            return LoginProtectionStatus.Locked(ipLockSeconds);

        return LoginProtectionStatus.Allowed();
    }

    public async Task RecordFailureAsync(
        string normalizedEmail,
        string clientIp,
        CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var emailFails = await IncrementWithWindowAsync(db, EmailFailKey(normalizedEmail));
        var ipFails = await IncrementWithWindowAsync(db, IpFailKey(clientIp));

        if (emailFails >= _settings.MaxFailedAttemptsPerEmail)
        {
            await SetLockAsync(db, EmailLockKey(normalizedEmail));
        }

        if (ipFails >= _settings.MaxFailedAttemptsPerIp)
        {
            await SetLockAsync(db, IpLockKey(clientIp));
        }

        await ApplyBackoffAsync(Math.Max(emailFails, ipFails), cancellationToken);
    }

    public async Task RecordSuccessAsync(
        string normalizedEmail,
        string clientIp,
        CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(
        [
            EmailFailKey(normalizedEmail),
            IpFailKey(clientIp),
            EmailLockKey(normalizedEmail),
            IpLockKey(clientIp)
        ]);
    }

    private async Task<long> IncrementWithWindowAsync(IDatabase db, string key)
    {
        var count = await db.StringIncrementAsync(key);
        if (count == 1)
        {
            await db.KeyExpireAsync(key, TimeSpan.FromSeconds(_settings.FailCounterWindowSeconds));
        }

        return count;
    }

    private async Task SetLockAsync(IDatabase db, string key)
    {
        await db.StringSetAsync(
            key,
            "1",
            TimeSpan.FromMinutes(_settings.LockoutMinutes),
            When.Always);
    }

    private async Task<int> GetLockRemainingSecondsAsync(IDatabase db, string key)
    {
        var ttl = await db.KeyTimeToLiveAsync(key);
        if (ttl is null || ttl.Value <= TimeSpan.Zero)
            return 0;

        return (int)Math.Ceiling(ttl.Value.TotalSeconds);
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
}
