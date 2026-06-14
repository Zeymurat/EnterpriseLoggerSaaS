using EnterpriseLogger.Domain.Enums;

namespace EnterpriseLogger.Application.Common.Interfaces;

public interface ICurrentUserProvider
{
    int? UserId { get; }
    int? TenantId { get; }
    string? Email { get; }
    TenantUserRole? Role { get; }
    IReadOnlyList<string> Permissions { get; }
    bool IsAuthenticated { get; }
}
