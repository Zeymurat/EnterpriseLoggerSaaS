namespace EnterpriseLogger.Application.Features.Users.Dtos;

public record UserResponseDto(
    int Id,
    string Email,
    string Phone,
    string Role,
    bool IsActive,
    IReadOnlyList<string> Permissions);

public record InviteUserRequest(
    string Email,
    string Phone,
    string Role,
    IReadOnlyList<string>? Permissions = null);

public record InviteUserResponseDto(
    UserResponseDto User,
    string TemporaryPassword);

public record UpdateUserPermissionsRequest(IReadOnlyList<string> Permissions);

public record UpdateUserRoleRequest(string Role);
