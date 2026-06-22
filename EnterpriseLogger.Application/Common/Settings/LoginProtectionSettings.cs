namespace EnterpriseLogger.Application.Common.Settings;

public class LoginProtectionSettings
{
    public int MaxFailedAttemptsPerEmail { get; init; } = 10;

    public int MaxFailedAttemptsPerIp { get; init; } = 30;

    public int LockoutMinutes { get; init; } = 15;

    public int FailCounterWindowSeconds { get; init; } = 900;

    public int BackoffStartAfterAttempts { get; init; } = 3;

    public int MaxBackoffSeconds { get; init; } = 8;
}
