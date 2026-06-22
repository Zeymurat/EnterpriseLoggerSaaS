namespace EnterpriseLogger.Application.Features.Platform.Auth.Dtos;

public record PlatformLoginRequest(string Email, string Password);

public record PlatformLoginResponse(
    string AccessToken,
    int ExpiresIn,
    PlatformAdminInfoDto Admin);

public record PlatformAdminInfoDto(int Id, string Email);
