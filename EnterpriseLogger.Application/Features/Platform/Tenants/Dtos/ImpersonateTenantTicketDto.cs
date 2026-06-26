namespace EnterpriseLogger.Application.Features.Platform.Tenants.Dtos;

public record ImpersonateTenantTicketDto(string Ticket, int ExpiresInSeconds);
