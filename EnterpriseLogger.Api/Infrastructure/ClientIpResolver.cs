namespace EnterpriseLogger.Api.Infrastructure;

public static class ClientIpResolver
{
    public static string GetClientIp(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
            return forwardedFor.Split(',')[0].Trim();

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
