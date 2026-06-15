using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Users.Dtos;
using EnterpriseLogger.Application.Features.Users.Queries;
using EnterpriseLogger.Domain.Entities;
using EnterpriseLogger.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Users.Commands;

public class UpdateUserPermissionsCommand
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserProvider _currentUser;
    private readonly IValidator<UpdateUserPermissionsRequest> _validator;

    public UpdateUserPermissionsCommand(
        IApplicationDbContext context,
        ICurrentUserProvider currentUser,
        IValidator<UpdateUserPermissionsRequest> validator)
    {
        _context = context;
        _currentUser = currentUser;
        _validator = validator;
    }

    public async Task<Result<UserResponseDto>> ExecuteAsync(
        int userId,
        UpdateUserPermissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result<UserResponseDto>.Failure($"Validasyon hatası: {errors}");
        }

        if (!_currentUser.IsAuthenticated || _currentUser.TenantId is null)
            return Result<UserResponseDto>.Forbidden("Kimlik doğrulama gerekli.");

        var user = await _context.Users
            .Include(u => u.UserPermissions)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null || user.TenantId != _currentUser.TenantId)
            return Result<UserResponseDto>.NotFound("Kullanıcı bulunamadı.");

        if (user.Role != TenantUserRole.User)
            return Result<UserResponseDto>.Failure("İzin güncellemesi yalnızca User rolündeki kullanıcılar için yapılabilir.");

        var permissionCodes = request.Permissions.Distinct(StringComparer.Ordinal).ToList();
        var invalidCodes = permissionCodes.Where(c => !PermissionCodes.All.Contains(c)).ToList();
        if (invalidCodes.Count > 0)
            return Result<UserResponseDto>.Failure($"Geçersiz izin kodları: {string.Join(", ", invalidCodes)}");

        var permissions = await _context.Permissions
            .Where(p => permissionCodes.Contains(p.Code))
            .ToListAsync(cancellationToken);

        _context.UserPermissions.RemoveRange(user.UserPermissions);
        user.UserPermissions.Clear();

        foreach (var permission in permissions)
        {
            user.UserPermissions.Add(new UserPermission
            {
                UserId = user.Id,
                PermissionId = permission.Id
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        user = await _context.Users
            .AsNoTracking()
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .FirstAsync(u => u.Id == userId, cancellationToken);

        return Result<UserResponseDto>.Success(UserDtoMapper.ToResponse(user));
    }
}

public class UpdateUserPermissionsRequestValidator : AbstractValidator<UpdateUserPermissionsRequest>
{
    public UpdateUserPermissionsRequestValidator()
    {
        RuleFor(x => x.Permissions)
            .NotNull().WithMessage("İzin listesi boş olamaz.")
            .Must(p => p.Count > 0).WithMessage("En az bir izin belirtilmelidir.");
    }
}
