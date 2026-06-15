using EnterpriseLogger.Application.Common;
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

public class InviteUserCommand
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserProvider _currentUser;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IValidator<InviteUserRequest> _validator;

    public InviteUserCommand(
        IApplicationDbContext context,
        ICurrentUserProvider currentUser,
        IPasswordHasher passwordHasher,
        IValidator<InviteUserRequest> validator)
    {
        _context = context;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
        _validator = validator;
    }

    public async Task<Result<InviteUserResponseDto>> ExecuteAsync(
        InviteUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result<InviteUserResponseDto>.Failure($"Validasyon hatası: {errors}");
        }

        if (!_currentUser.IsAuthenticated || _currentUser.TenantId is null || _currentUser.Role is null)
            return Result<InviteUserResponseDto>.Forbidden("Kimlik doğrulama gerekli.");

        if (!Enum.TryParse<TenantUserRole>(request.Role, ignoreCase: true, out var targetRole)
            || targetRole is TenantUserRole.Root)
        {
            return Result<InviteUserResponseDto>.Failure("Davet edilebilir roller: Admin veya User.");
        }

        if (targetRole == TenantUserRole.Admin && _currentUser.Role != TenantUserRole.Root)
            return Result<InviteUserResponseDto>.Forbidden("Admin daveti yalnızca Root kullanıcı tarafından yapılabilir.");

        if (!PhoneNormalizer.TryNormalize(request.Phone, out var phone))
            return Result<InviteUserResponseDto>.Failure("Validasyon hatası: Geçerli bir telefon numarası giriniz.");

        var email = request.Email.Trim().ToLowerInvariant();
        var tenantId = _currentUser.TenantId.Value;

        var permissionCodes = request.Permissions?.Distinct(StringComparer.Ordinal).ToList() ?? [];
        if (targetRole == TenantUserRole.User && permissionCodes.Count == 0)
            return Result<InviteUserResponseDto>.Failure("User rolü için en az bir izin belirtilmelidir.");

        if (targetRole == TenantUserRole.Admin && permissionCodes.Count > 0)
            return Result<InviteUserResponseDto>.Failure("Admin rolü için izin listesi gönderilmemelidir.");

        var invalidCodes = permissionCodes.Where(c => !PermissionCodes.All.Contains(c)).ToList();
        if (invalidCodes.Count > 0)
            return Result<InviteUserResponseDto>.Failure($"Geçersiz izin kodları: {string.Join(", ", invalidCodes)}");

        var existingUser = await _context.Users
            .Include(u => u.UserPermissions)
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == email, cancellationToken);

        if (existingUser is { IsActive: true })
            return Result<InviteUserResponseDto>.Conflict("Bu e-posta adresi bu şirkette zaten kayıtlı.");

        var phoneOwner = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Phone == phone, cancellationToken);

        if (phoneOwner is not null && (existingUser is null || phoneOwner.Id != existingUser.Id))
            return Result<InviteUserResponseDto>.Conflict("Bu telefon numarası bu şirkette zaten kayıtlı.");

        var temporaryPassword = TemporaryPasswordGenerator.Generate();
        var passwordHash = _passwordHasher.Hash(temporaryPassword);

        User user;
        if (existingUser is { IsActive: false })
        {
            user = existingUser;
            user.Phone = phone;
            user.PasswordHash = passwordHash;
            user.Role = targetRole;
            user.IsActive = true;
            _context.UserPermissions.RemoveRange(user.UserPermissions);
            user.UserPermissions.Clear();
        }
        else
        {
            user = new User
            {
                TenantId = tenantId,
                Email = email,
                Phone = phone,
                PasswordHash = passwordHash,
                Role = targetRole,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
        }

        if (targetRole == TenantUserRole.User)
        {
            var permissions = await _context.Permissions
                .Where(p => permissionCodes.Contains(p.Code))
                .ToListAsync(cancellationToken);

            foreach (var permission in permissions)
            {
                user.UserPermissions.Add(new UserPermission
                {
                    User = user,
                    Permission = permission
                });
            }
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result<InviteUserResponseDto>.Conflict("Bu e-posta veya telefon numarası bu şirkette zaten kayıtlı.");
        }

        var saved = await _context.Users
            .AsNoTracking()
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .FirstAsync(u => u.Id == user.Id, cancellationToken);

        return Result<InviteUserResponseDto>.Success(
            new InviteUserResponseDto(UserDtoMapper.ToResponse(saved), temporaryPassword));
    }
}

public class InviteUserRequestValidator : AbstractValidator<InviteUserRequest>
{
    public InviteUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta adresi boş olamaz.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
            .MaximumLength(256);

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Telefon numarası boş olamaz.")
            .Must(phone => PhoneNormalizer.TryNormalize(phone, out _))
            .WithMessage("Geçerli bir telefon numarası giriniz.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Rol boş olamaz.");
    }
}
