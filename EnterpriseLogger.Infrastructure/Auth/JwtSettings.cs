namespace EnterpriseLogger.Infrastructure.Auth;

public class JwtSettings
{
    public const string TestingSecret = "EnterpriseLogger-Integration-Test-Secret-32chars!";

    public string Secret { get; init; } = string.Empty;
    public string Issuer { get; init; } = "EnterpriseLogger";
    public string Audience { get; init; } = "EnterpriseLogger.Api";
    public int AccessTokenExpiryMinutes { get; init; } = 60;
    public int MaxSessionHours { get; init; } = 8;
}
