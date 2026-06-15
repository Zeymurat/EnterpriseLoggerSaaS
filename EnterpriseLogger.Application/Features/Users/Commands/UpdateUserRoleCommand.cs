using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Users.Dtos;
using EnterpriseLogger.Application.Features.Users.Queries;
using EnterpriseLogger.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Users.Commands;

public class UpdateUserRoleCommand
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserProvider _currentUser;
    private readonly IValidator<UpdateUserRoleRequest> _validator;

    public UpdateUserRoleCommand(
        IApplicationDbContext context,
        ICurrentUserProvider currentUser,
        IValidator<UpdateUserRoleRequest> validator)
    {
        _context = context;
        _currentUser = currentUser;
        _validator = validator;
    }

    public async Task<Result<UserResponseDto>> ExecuteAsync(
        int userId,
        UpdateUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result<UserResponseDto>.Failure($"Validasyon hatası: {errors}");
        }

        if (_currentUser.Role != TenantUserRole.Root)
            return Result<UserResponseDto>.Forbidden("Rol güncellemesi yalnızca Root kullanıcı tarafından yapılabilir.");

        if (!Enum.TryParse<TenantUserRole>(request.Role, ignoreCase: true, out var newRole)
            || newRole is TenantUserRole.Root)
        {
            return Result<UserResponseDto>.Failure("Atanabilir roller: Admin veya User.");
        }

        var user = await _context.Users
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null || user.TenantId != _currentUser.TenantId)
            return Result<UserResponseDto>.NotFound("Kullanıcı bulunamadı.");

        if (user.Role == TenantUserRole.Root)
            return Result<UserResponseDto>.Forbidden("Root kullanıcının rolü değiştirilemez.");

        if (newRole == TenantUserRole.User && user.Role == TenantUserRole.Admin)
        {
            return Result<UserResponseDto>.Failure(
                "Admin kullanıcı doğrudan User rolüne düşürülemez. Önce pasife alın.");
        }

        user.Role = newRole;

        if (newRole == TenantUserRole.Admin)
            _context.UserPermissions.RemoveRange(user.UserPermissions);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<UserResponseDto>.Success(UserDtoMapper.ToResponse(user));
    }
}

public class UpdateUserRoleRequestValidator : AbstractValidator<UpdateUserRoleRequest>
{
    public UpdateUserRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Rol boş olamaz.");
    }
}
