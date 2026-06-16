namespace EnterpriseLogger.Application.Features.Auth.Dtos;

public record LoginRequest(string Email, string Password, string? TenantName = null);

public record TenantLoginOptionDto(
    string TenantName,
    string Role,
    string? DisplayName = null);

public record LoginResponse(
    string AccessToken,
    int ExpiresIn,
    UserInfoDto User);

public record UserInfoDto(
    int Id,
    string Email,
    string Phone,
    string Role,
    int TenantId,
    string TenantName,
    IReadOnlyList<string> Permissions);
