using System.Security.Claims;
using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Domain.Enums;

namespace EnterpriseLogger.Api.Services;

public class HttpContextCurrentUserProvider : ICurrentUserProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUserProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId
    {
        get
        {
            var sub = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(sub, out var id) ? id : null;
        }
    }

    public int? TenantId
    {
        get
        {
            var tenantId = _httpContextAccessor.HttpContext?.User.FindFirstValue(AuthClaimTypes.TenantId);
            return int.TryParse(tenantId, out var id) ? id : null;
        }
    }

    public string? Email =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

    public TenantUserRole? Role
    {
        get
        {
            var role = _httpContextAccessor.HttpContext?.User.FindFirstValue(AuthClaimTypes.Role)
                ?? _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<TenantUserRole>(role, ignoreCase: true, out var parsed) ? parsed : null;
        }
    }

    public IReadOnlyList<string> Permissions =>
        _httpContextAccessor.HttpContext?.User
            .FindAll(c => c.Type == AuthClaimTypes.Permission || c.Type == "permissions")
            .Select(c => c.Value)
            .Distinct(StringComparer.Ordinal)
            .ToList()
        ?? [];

    public DateTime? SessionStartedAtUtc
    {
        get
        {
            var raw = _httpContextAccessor.HttpContext?.User.FindFirstValue(AuthClaimTypes.SessionStartedAt);
            if (string.IsNullOrWhiteSpace(raw) || !long.TryParse(raw, out var unixSeconds))
                return null;

            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
        }
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}
