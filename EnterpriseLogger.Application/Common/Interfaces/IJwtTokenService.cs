namespace EnterpriseLogger.Application.Common.Interfaces;

public interface IJwtTokenService
{
    JwtTokenResult GenerateToken(
        int userId,
        string email,
        int tenantId,
        string role,
        IReadOnlyList<string> permissions,
        DateTime sessionStartedAtUtc);

    JwtTokenResult GeneratePlatformToken(int platformAdminId, string email);
}

public record JwtTokenResult(string AccessToken, int ExpiresInSeconds);
