namespace EnterpriseLogger.Application.Features.Tenants.Dtos;

public record CreateTenantRequest(string Name, string ApiKey);

public record TenantResponseDto(int Id, string Name, string ApiKey, bool IsActive, DateTime CreatedAt);
