using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace EnterpriseLogger.Infrastructure.Auth;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(JwtSettings settings)
    {
        _settings = settings;
    }

    public JwtTokenResult GenerateToken(
        int userId,
        string email,
        int tenantId,
        string role,
        IReadOnlyList<string> permissions,
        DateTime sessionStartedAtUtc)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes);
        var sessionStartedUnix = new DateTimeOffset(
            DateTime.SpecifyKind(sessionStartedAtUtc, DateTimeKind.Utc)).ToUnixTimeSeconds();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(AuthClaimTypes.TenantId, tenantId.ToString()),
            new(AuthClaimTypes.Role, role),
            new(AuthClaimTypes.SessionStartedAt, sessionStartedUnix.ToString()),
        };

        claims.AddRange(permissions.Select(p => new Claim(AuthClaimTypes.Permission, p)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new JwtTokenResult(accessToken, (int)(expiresAt - DateTime.UtcNow).TotalSeconds);
    }

    public JwtTokenResult GeneratePlatformToken(int platformAdminId, string email)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, platformAdminId.ToString()),
            new(ClaimTypes.Email, email),
            new(AuthClaimTypes.IsPlatformAdmin, "true"),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new JwtTokenResult(accessToken, (int)(expiresAt - DateTime.UtcNow).TotalSeconds);
    }
}
