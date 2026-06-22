using EnterpriseLogger.Api.Infrastructure;
using EnterpriseLogger.Application.Common.Interfaces;

namespace EnterpriseLogger.Api.Services;

public class HttpContextLoginClientContext : ILoginClientContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextLoginClientContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string ClientIp
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            return context is null ? "unknown" : ClientIpResolver.GetClientIp(context);
        }
    }
}
