using System.Security.Claims;
using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Interfaces;

namespace EnterpriseLogger.Api.Services;

public class HttpContextCurrentTenantProvider : ICurrentTenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? TenantId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
                return null;

            if (httpContext.Items.TryGetValue(TenantAuthConstants.TenantIdItemKey, out var value))
            {
                return value switch
                {
                    int id => id,
                    _ => null
                };
            }

            var tenantIdClaim = httpContext.User.FindFirst(AuthClaimTypes.TenantId)?.Value;
            return int.TryParse(tenantIdClaim, out var tenantId) ? tenantId : null;
        }
    }
}
