namespace EnterpriseLogger.Application.Common.Interfaces;

public interface IJwtTokenService
{
    JwtTokenResult GenerateToken(
        int userId,
        string email,
        int tenantId,
        string role,
        IReadOnlyList<string> permissions);
}

public record JwtTokenResult(string AccessToken, int ExpiresInSeconds);
