namespace EnterpriseLogger.Application.Common.Constants;

public static class AuthClaimTypes
{
    public const string TenantId = "tenantId";
    public const string Role = "role";
    public const string Permission = "permission";
    public const string SessionStartedAt = "session_started_at";
}

public static class AuthSchemeNames
{
    public const string Bearer = "Bearer";
    public const string ApiKey = "ApiKey";
}
