using EnterpriseLogger.Api.Infrastructure;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Settings;

namespace EnterpriseLogger.Api.Middleware;

public class PublicEndpointRateLimitMiddleware
{
    private const string AuthLoginBucket = "auth-login";
    private const string TenantRegisterBucket = "tenant-register";

    private readonly RequestDelegate _next;

    public PublicEndpointRateLimitMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IRateLimiter rateLimiter,
        RateLimitSettings settings)
    {
        if (!TryResolveTarget(context, out var bucket, out var policy, settings))
        {
            await _next(context);
            return;
        }

        var clientIp = ClientIpResolver.GetClientIp(context);
        var result = await rateLimiter.TryAcquireAsync(
            $"ip:{clientIp}",
            bucket,
            policy,
            context.RequestAborted);

        if (!result.IsAllowed)
        {
            context.Response.Headers.RetryAfter = result.RetryAfterSeconds.ToString();
            await ApiProblemDetails.WriteAsync(
                context,
                ApiProblemDetails.TooManyRequests(GetDetailMessage(bucket)));
            return;
        }

        await _next(context);
    }

    private static bool TryResolveTarget(
        HttpContext context,
        out string bucket,
        out RateLimitPolicy policy,
        RateLimitSettings settings)
    {
        bucket = string.Empty;
        policy = RateLimitPolicy.Disabled;

        if (!HttpMethods.IsPost(context.Request.Method))
            return false;

        if (context.Request.Path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase))
        {
            bucket = AuthLoginBucket;
            policy = settings.AuthLoginPolicy;
            return true;
        }

        if (context.Request.Path.Equals("/api/tenants", StringComparison.OrdinalIgnoreCase))
        {
            bucket = TenantRegisterBucket;
            policy = settings.TenantRegisterPolicy;
            return true;
        }

        return false;
    }

    private static string GetDetailMessage(string bucket) =>
        bucket switch
        {
            AuthLoginBucket =>
                "Çok fazla giriş denemesi. Lütfen kısa süre sonra tekrar deneyin.",
            TenantRegisterBucket =>
                "Çok fazla kayıt denemesi. Lütfen kısa süre sonra tekrar deneyin.",
            _ => "İstek limiti aşıldı. Lütfen kısa süre sonra tekrar deneyin."
        };
}
