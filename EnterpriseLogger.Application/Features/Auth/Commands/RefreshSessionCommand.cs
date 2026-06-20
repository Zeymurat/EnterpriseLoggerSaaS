using EnterpriseLogger.Application.Common.Authorization;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Common.Settings;
using EnterpriseLogger.Application.Features.Auth.Dtos;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Auth.Commands;

public class RefreshSessionCommand
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserProvider _currentUser;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly SessionSettings _sessionSettings;

    public RefreshSessionCommand(
        IApplicationDbContext context,
        ICurrentUserProvider currentUser,
        IJwtTokenService jwtTokenService,
        SessionSettings sessionSettings)
    {
        _context = context;
        _currentUser = currentUser;
        _jwtTokenService = jwtTokenService;
        _sessionSettings = sessionSettings;
    }

    public async Task<Result<LoginResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<LoginResponse>.Failure("Oturum gerekli. Lütfen tekrar giriş yapın.");

        var sessionStartedAtUtc = _currentUser.SessionStartedAtUtc ?? DateTime.UtcNow;

        if (_sessionSettings.MaxSessionHours > 0)
        {
            var maxSessionEnd = sessionStartedAtUtc.AddHours(_sessionSettings.MaxSessionHours);
            if (DateTime.UtcNow >= maxSessionEnd)
            {
                return Result<LoginResponse>.Failure(
                    "Maksimum oturum süresi doldu. Lütfen tekrar giriş yapın.");
            }
        }

        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Tenant)
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId.Value && u.IsActive, cancellationToken);

        if (user is null)
            return Result<LoginResponse>.Failure("Oturum gerekli. Lütfen tekrar giriş yapın.");

        if (!user.Tenant.IsActive)
            return Result<LoginResponse>.Failure("Hesap pasif durumda.");

        var permissions = PermissionResolver.Resolve(
            user.Role,
            user.UserPermissions.Select(up => up.Permission.Code));

        var token = _jwtTokenService.GenerateToken(
            user.Id,
            user.Email,
            user.TenantId,
            user.Role.ToString(),
            permissions,
            sessionStartedAtUtc);

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
}
