using EnterpriseLogger.Api.Infrastructure;
using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Api.Middleware;

/// <summary>
/// Gelen istekteki X-Api-Key header'ını veritabanındaki Tenant ile eşleştirir.
/// Başarılı doğrulamada TenantId istek yaşam döngüsüne yazılır.
/// </summary>
public class TenantMappingMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMappingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IApplicationDbContext dbContext)
    {
        if (IsExempt(context))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(TenantAuthConstants.ApiKeyHeaderName, out var apiKeyValues)
            || string.IsNullOrWhiteSpace(apiKeyValues.FirstOrDefault()))
        {
            await ApiProblemDetails.WriteAsync(context,
                ApiProblemDetails.Unauthorized("X-Api-Key header zorunludur."));
            return;
        }

        var apiKey = apiKeyValues.ToString().Trim();

        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ApiKey == apiKey, context.RequestAborted);

        if (tenant is null)
        {
            await ApiProblemDetails.WriteAsync(
                context,
                ApiProblemDetails.Unauthorized("Geçersiz API Key."));
            return;
        }

        if (!tenant.IsActive)
        {
            await ApiProblemDetails.WriteAsync(
                context,
                ApiProblemDetails.Forbidden("Tenant hesabı pasif durumda."));
            return;
        }

        context.Items[TenantAuthConstants.TenantIdItemKey] = tenant.Id;

        await _next(context);
    }

    private static bool IsExempt(HttpContext context)
    {
        var path = context.Request.Path;

        if (path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase))
            return true;

        if (path.StartsWithSegments("/api/_debug", StringComparison.OrdinalIgnoreCase))
            return true;

        if (context.Request.Method.Equals(HttpMethods.Post, StringComparison.OrdinalIgnoreCase)
            && path.StartsWithSegments("/api/tenants", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }
}
