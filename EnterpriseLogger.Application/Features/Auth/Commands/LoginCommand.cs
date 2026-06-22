using EnterpriseLogger.Application.Common.Authorization;
using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Auth.Dtos;
using EnterpriseLogger.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Auth.Commands;

public class LoginCommand
{
    private const string GenericAuthFailureMessage = "E-posta veya şifre hatalı.";

    private readonly IApplicationDbContext _context;
    private readonly IValidator<LoginRequest> _validator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILoginProtectionService _loginProtection;
    private readonly ILoginClientContext _loginClientContext;

    public LoginCommand(
        IApplicationDbContext context,
        IValidator<LoginRequest> validator,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ILoginProtectionService loginProtection,
        ILoginClientContext loginClientContext)
    {
        _context = context;
        _validator = validator;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _loginProtection = loginProtection;
        _loginClientContext = loginClientContext;
    }

    public async Task<Result<LoginResponse>> ExecuteAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result<LoginResponse>.Failure($"Validasyon hatası: {errors}");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var clientIp = _loginClientContext.ClientIp;

        var protectionStatus = await _loginProtection.CheckAsync(email, clientIp, cancellationToken);
        if (!protectionStatus.IsAllowed)
        {
            return Result<LoginResponse>.RateLimited(
                protectionStatus.Message ?? LoginProtectionStatus.LockoutMessage,
                protectionStatus.RetryAfterSeconds);
        }

        var candidates = await _context.Users
            .AsNoTracking()
            .Include(u => u.Tenant)
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .Where(u => u.Email == email && u.IsActive)
            .ToListAsync(cancellationToken);

        var matchingUsers = candidates
            .Where(u => _passwordHasher.Verify(request.Password, u.PasswordHash))
            .ToList();

        if (matchingUsers.Count == 0)
        {
            await _loginProtection.RecordFailureAsync(email, clientIp, cancellationToken);
            return Result<LoginResponse>.Failure(GenericAuthFailureMessage);
        }

        if (matchingUsers.Count > 1 && string.IsNullOrWhiteSpace(request.TenantName))
        {
            await _loginProtection.RecordSuccessAsync(email, clientIp, cancellationToken);

            var tenantOptions = matchingUsers
                .Select(u => new TenantLoginOptionDto(
                    u.Tenant.Name,
                    u.Role.ToString()))
                .OrderBy(option => option.TenantName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return Result<LoginResponse>.AmbiguousTenant(
                "Bu e-posta birden fazla şirkete kayıtlı. Lütfen şirket seçin.",
                tenantOptions);
        }

        var user = ResolveUser(matchingUsers, request.TenantName);
        if (user is null)
        {
            if (!string.IsNullOrWhiteSpace(request.TenantName)
                && matchingUsers.Count > 1
                && HasAmbiguousTenantName(matchingUsers, request.TenantName))
            {
                return Result<LoginResponse>.Failure(
                    "Belirtilen şirket adı birden fazla kayıtla eşleşiyor. Şirket adını tam olarak girin (büyük/küçük harf duyarlı).");
            }

            await _loginProtection.RecordFailureAsync(email, clientIp, cancellationToken);
            return Result<LoginResponse>.Failure(GenericAuthFailureMessage);
        }

        if (!user.Tenant.IsActive)
            return Result<LoginResponse>.Failure("Hesap pasif durumda.");

        await _loginProtection.RecordSuccessAsync(email, clientIp, cancellationToken);

        var permissions = PermissionResolver.Resolve(
            user.Role,
            user.UserPermissions.Select(up => up.Permission.Code));

        var token = _jwtTokenService.GenerateToken(
            user.Id,
            user.Email,
            user.TenantId,
            user.Role.ToString(),
            permissions,
            DateTime.UtcNow);

        await UpdateLastLoginAsync(user.Id, cancellationToken);

        var response = new LoginResponse(
            token.AccessToken,
            token.ExpiresInSeconds,
            new UserInfoDto(
                user.Id,
                user.Email,
                user.Phone,
                user.Role.ToString(),
                user.TenantId,
                user.Tenant.Name,
                permissions));

        return Result<LoginResponse>.Success(response);
    }

    private static User? ResolveUser(IReadOnlyList<User> matchingUsers, string? tenantName)
    {
        if (matchingUsers.Count == 1)
            return matchingUsers[0];

        if (string.IsNullOrWhiteSpace(tenantName))
            return null;

        var normalizedTenantName = tenantName.Trim();

        var exactMatches = matchingUsers
            .Where(u => u.Tenant.Name.Equals(normalizedTenantName, StringComparison.Ordinal))
            .ToList();

        if (exactMatches.Count == 1)
            return exactMatches[0];

        var caseInsensitiveMatches = matchingUsers
            .Where(u => u.Tenant.Name.Equals(normalizedTenantName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return caseInsensitiveMatches.Count == 1 ? caseInsensitiveMatches[0] : null;
    }

    private static bool HasAmbiguousTenantName(IReadOnlyList<User> matchingUsers, string tenantName)
    {
        var normalizedTenantName = tenantName.Trim();
        var exactMatches = matchingUsers
            .Where(u => u.Tenant.Name.Equals(normalizedTenantName, StringComparison.Ordinal))
            .Count();

        if (exactMatches == 1)
            return false;

        return matchingUsers.Count(u =>
            u.Tenant.Name.Equals(normalizedTenantName, StringComparison.OrdinalIgnoreCase)) > 1;
    }

    private async Task UpdateLastLoginAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return;

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta adresi boş olamaz.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
            .MaximumLength(256).WithMessage("E-posta adresi en fazla 256 karakter olabilir.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre boş olamaz.");

        RuleFor(x => x.TenantName)
            .MaximumLength(150).WithMessage("Şirket adı en fazla 150 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.TenantName));
    }
}
