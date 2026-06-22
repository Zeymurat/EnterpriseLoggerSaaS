namespace EnterpriseLogger.Application.Common.Settings;

public class RateLimitSettings
{
    public int LogIngestRequestsPerWindow { get; init; } = 1000;

    public int LogIngestWindowSeconds { get; init; } = 60;
}
