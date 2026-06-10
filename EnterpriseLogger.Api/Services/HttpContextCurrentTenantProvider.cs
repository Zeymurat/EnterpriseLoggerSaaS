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
            var items = _httpContextAccessor.HttpContext?.Items;
            if (items is null || !items.TryGetValue(TenantAuthConstants.TenantIdItemKey, out var value))
                return null;

            return value switch
            {
                int id => id,
                _ => null
            };
        }
    }
}
