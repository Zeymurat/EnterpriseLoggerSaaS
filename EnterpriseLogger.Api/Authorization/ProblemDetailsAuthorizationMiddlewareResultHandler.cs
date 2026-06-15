using EnterpriseLogger.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace EnterpriseLogger.Api.Authorization;

/// <summary>
/// Yetkilendirme hatalarını RFC 7807 Problem Details formatında döner.
/// </summary>
public class ProblemDetailsAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Challenged)
        {
            await ApiProblemDetails.WriteAsync(
                context,
                ApiProblemDetails.Unauthorized("Kimlik doğrulama gerekli. X-Api-Key veya Bearer JWT sağlayın."));
            return;
        }

        if (authorizeResult.Forbidden)
        {
            await ApiProblemDetails.WriteAsync(
                context,
                ApiProblemDetails.Forbidden("Bu işlem için yetkiniz yok."));
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
