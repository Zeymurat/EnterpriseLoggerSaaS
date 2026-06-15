using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Users.Dtos;
using EnterpriseLogger.Application.Features.Users.Queries;
using EnterpriseLogger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Users.Commands;

public class DeactivateUserCommand
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserProvider _currentUser;

    public DeactivateUserCommand(IApplicationDbContext context, ICurrentUserProvider currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<UserResponseDto>> ExecuteAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.TenantId is null || _currentUser.Role is null)
            return Result<UserResponseDto>.Forbidden("Kimlik doğrulama gerekli.");

        var user = await _context.Users
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null || user.TenantId != _currentUser.TenantId)
            return Result<UserResponseDto>.NotFound("Kullanıcı bulunamadı.");

        if (!user.IsActive)
            return Result<UserResponseDto>.Conflict("Kullanıcı zaten pasif durumda.");

        if (user.Role == TenantUserRole.Root)
            return Result<UserResponseDto>.Forbidden("Root kullanıcı pasife alınamaz.");

        if (_currentUser.Role == TenantUserRole.Admin && user.Role != TenantUserRole.User)
            return Result<UserResponseDto>.Forbidden("Admin yalnızca User rolündeki kullanıcıları pasife alabilir.");

        if (user.Id == _currentUser.UserId)
            return Result<UserResponseDto>.Forbidden("Kendi hesabınızı pasife alamazsınız.");

        user.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);

        return Result<UserResponseDto>.Success(UserDtoMapper.ToResponse(user));
    }
}
