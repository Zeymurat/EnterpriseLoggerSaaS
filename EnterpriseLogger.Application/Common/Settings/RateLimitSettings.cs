using EnterpriseLogger.Application.Common.Interfaces;

namespace EnterpriseLogger.Application.Common.Settings;

public class RateLimitSettings
{
    public int LogIngestRequestsPerWindow { get; init; } = 1000;

    public int LogIngestWindowSeconds { get; init; } = 60;

    public int AuthLoginRequestsPerWindow { get; init; } = 20;

    public int AuthLoginWindowSeconds { get; init; } = 60;

    public int TenantRegisterRequestsPerWindow { get; init; } = 5;

    public int TenantRegisterWindowSeconds { get; init; } = 60;

    public RateLimitPolicy LogIngestPolicy =>
        new(LogIngestRequestsPerWindow, LogIngestWindowSeconds);

    public RateLimitPolicy AuthLoginPolicy =>
        new(AuthLoginRequestsPerWindow, AuthLoginWindowSeconds);

    public RateLimitPolicy TenantRegisterPolicy =>
        new(TenantRegisterRequestsPerWindow, TenantRegisterWindowSeconds);
}
