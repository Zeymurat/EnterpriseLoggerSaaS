using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Platform.Auth.Dtos;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Auth.Commands;

public class PlatformLoginCommand
{
    private const string GenericAuthFailureMessage = "E-posta veya şifre hatalı.";

    private readonly IApplicationDbContext _context;
    private readonly IValidator<PlatformLoginRequest> _validator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILoginProtectionService _loginProtection;
    private readonly ILoginClientContext _loginClientContext;

    public PlatformLoginCommand(
        IApplicationDbContext context,
        IValidator<PlatformLoginRequest> validator,
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

    public async Task<Result<PlatformLoginResponse>> ExecuteAsync(
        PlatformLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result<PlatformLoginResponse>.Failure($"Validasyon hatası: {errors}");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var clientIp = _loginClientContext.ClientIp;

        var protectionStatus = await _loginProtection.CheckAsync(email, clientIp, cancellationToken);
        if (!protectionStatus.IsAllowed)
        {
            return Result<PlatformLoginResponse>.RateLimited(
                protectionStatus.Message ?? LoginProtectionStatus.LockoutMessage,
                protectionStatus.RetryAfterSeconds);
        }

        var admin = await _context.PlatformAdmins
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Email == email && a.IsActive, cancellationToken);

        if (admin is null || !_passwordHasher.Verify(request.Password, admin.PasswordHash))
        {
            var lockedResult = await RecordAuthFailureAsync(email, clientIp, cancellationToken);
            return lockedResult ?? Result<PlatformLoginResponse>.Failure(GenericAuthFailureMessage);
        }

        await _loginProtection.RecordSuccessAsync(email, clientIp, cancellationToken);

        var token = _jwtTokenService.GeneratePlatformToken(admin.Id, admin.Email);
        await UpdateLastLoginAsync(admin.Id, cancellationToken);

        var response = new PlatformLoginResponse(
            token.AccessToken,
            token.ExpiresInSeconds,
            new PlatformAdminInfoDto(admin.Id, admin.Email));

        return Result<PlatformLoginResponse>.Success(response);
    }

    private async Task<Result<PlatformLoginResponse>?> RecordAuthFailureAsync(
        string email,
        string clientIp,
        CancellationToken cancellationToken)
    {
        await _loginProtection.RecordFailureAsync(email, clientIp, cancellationToken);

        var status = await _loginProtection.CheckAsync(email, clientIp, cancellationToken);
        if (!status.IsAllowed)
        {
            return Result<PlatformLoginResponse>.RateLimited(
                status.Message ?? LoginProtectionStatus.LockoutMessage,
                status.RetryAfterSeconds);
        }

        return null;
    }

    private async Task UpdateLastLoginAsync(int adminId, CancellationToken cancellationToken)
    {
        var admin = await _context.PlatformAdmins.FirstOrDefaultAsync(a => a.Id == adminId, cancellationToken);
        if (admin is null)
            return;

        admin.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class PlatformLoginRequestValidator : AbstractValidator<PlatformLoginRequest>
{
    public PlatformLoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta adresi boş olamaz.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
            .MaximumLength(256).WithMessage("E-posta adresi en fazla 256 karakter olabilir.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre boş olamaz.");
    }
}
