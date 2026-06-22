namespace EnterpriseLogger.Api.Infrastructure;

public static class ClientIpResolver
{
    public static string GetClientIp(HttpContext context)
    {
        if (TrustForwardedHeaders())
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
                return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Only enable behind a trusted reverse proxy (Nginx, load balancer).
    /// When false, clients cannot spoof X-Forwarded-For to bypass IP rate limits.
    /// </summary>
    public static bool TrustForwardedHeaders() =>
        string.Equals(
            Environment.GetEnvironmentVariable("TRUST_FORWARDED_HEADERS"),
            "true",
            StringComparison.OrdinalIgnoreCase);
}
