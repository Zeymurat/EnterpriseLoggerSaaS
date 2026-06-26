using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Platform.Auth.Dtos;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Auth.Commands;

public class PlatformRefreshSessionCommand
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserProvider _currentUser;
    private readonly IJwtTokenService _jwtTokenService;

    public PlatformRefreshSessionCommand(
        IApplicationDbContext context,
        ICurrentUserProvider currentUser,
        IJwtTokenService jwtTokenService)
    {
        _context = context;
        _currentUser = currentUser;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<PlatformLoginResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null || _currentUser.TenantId is not null)
            return Result<PlatformLoginResponse>.Failure("Oturum gerekli. Lütfen tekrar giriş yapın.");

        var admin = await _context.PlatformAdmins
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == _currentUser.UserId.Value && a.IsActive, cancellationToken);

        if (admin is null)
            return Result<PlatformLoginResponse>.Failure("Oturum gerekli. Lütfen tekrar giriş yapın.");

        var token = _jwtTokenService.GeneratePlatformToken(admin.Id, admin.Email);

        return Result<PlatformLoginResponse>.Success(
            new PlatformLoginResponse(
                token.AccessToken,
                token.ExpiresInSeconds,
                new PlatformAdminInfoDto(admin.Id, admin.Email)));
    }
}
