namespace EnterpriseLogger.Application.Common.Interfaces;

public interface ILoginProtectionService
{
    Task<LoginProtectionStatus> CheckAsync(
        string normalizedEmail,
        string clientIp,
        CancellationToken cancellationToken = default);

    Task RecordFailureAsync(
        string normalizedEmail,
        string clientIp,
        CancellationToken cancellationToken = default);

    Task RecordSuccessAsync(
        string normalizedEmail,
        string clientIp,
        CancellationToken cancellationToken = default);
}

public sealed record LoginProtectionStatus(bool IsAllowed, int RetryAfterSeconds = 0, string? Message = null)
{
    public const string LockoutMessage =
        "Çok fazla başarısız giriş denemesi. Lütfen kısa süre sonra tekrar deneyin.";

    public static LoginProtectionStatus Allowed() => new(true);

    public static LoginProtectionStatus Locked(int retryAfterSeconds) =>
        new(false, Math.Max(1, retryAfterSeconds), LockoutMessage);
}
