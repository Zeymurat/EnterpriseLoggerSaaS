using EnterpriseLogger.Application.Common.Authorization;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Users.Dtos;
using EnterpriseLogger.Domain.Entities;
using EnterpriseLogger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Users.Queries;

public class GetTenantUsersQuery
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserProvider _currentUser;

    public GetTenantUsersQuery(IApplicationDbContext context, ICurrentUserProvider currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<UserResponseDto>>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.TenantId is null)
            return Result<IReadOnlyList<UserResponseDto>>.Forbidden("Kimlik doğrulama gerekli.");

        var users = await _context.Users
            .AsNoTracking()
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .Where(u => u.TenantId == _currentUser.TenantId)
            .OrderBy(u => u.Role)
            .ThenBy(u => u.Email)
            .ToListAsync(cancellationToken);

        var response = users
            .Select(UserDtoMapper.ToResponse)
            .ToList();

        return Result<IReadOnlyList<UserResponseDto>>.Success(response);
    }
}

internal static class UserDtoMapper
{
    public static UserResponseDto ToResponse(User user)
    {
        var permissions = PermissionResolver.Resolve(
            user.Role,
            user.UserPermissions.Select(up => up.Permission.Code));

        return new UserResponseDto(
            user.Id,
            user.Email,
            user.Phone,
            user.Role.ToString(),
            user.IsActive,
            permissions);
    }
}
