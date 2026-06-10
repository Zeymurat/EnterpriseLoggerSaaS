namespace EnterpriseLogger.Application.Common.Constants;

public static class TenantAuthConstants
{
    public const string ApiKeyHeaderName = "X-Api-Key";

    /// <summary>
    /// HttpContext.Items anahtarı — middleware yazar, ICurrentTenantProvider okur.
    /// </summary>
    public const string TenantIdItemKey = "CurrentTenantId";
}
