using EnterpriseLogger.Api.Infrastructure;
using EnterpriseLogger.Application.Common.Interfaces;

namespace EnterpriseLogger.Api.Middleware;

public class LogIngestRateLimitMiddleware
{
    private const string LogIngestBucket = "log-ingest";

    private readonly RequestDelegate _next;

    public LogIngestRateLimitMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICurrentTenantProvider tenantProvider,
        IRateLimiter rateLimiter)
    {
        if (!ShouldRateLimit(context))
        {
            await _next(context);
            return;
        }

        if (!tenantProvider.IsResolved)
        {
            await _next(context);
            return;
        }

        var result = await rateLimiter.TryAcquireAsync(
            tenantProvider.TenantId!.Value,
            LogIngestBucket,
            context.RequestAborted);

        if (!result.IsAllowed)
        {
            context.Response.Headers.RetryAfter = result.RetryAfterSeconds.ToString();
            await ApiProblemDetails.WriteAsync(
                context,
                ApiProblemDetails.TooManyRequests(
                    "Tenant log ingestion limiti aşıldı. Lütfen kısa süre sonra tekrar deneyin."));
            return;
        }

        await _next(context);
    }

    private static bool ShouldRateLimit(HttpContext context) =>
        HttpMethods.IsPost(context.Request.Method)
        && context.Request.Path.StartsWithSegments("/api/logs", StringComparison.OrdinalIgnoreCase);
}
