using EnterpriseLogger.Application.Features.Auth.Dtos;

namespace EnterpriseLogger.Application.Common.Interfaces;

public interface IImpersonationTicketStore
{
    string CreateTicket(LoginResponse session, TimeSpan ttl);

    LoginResponse? ConsumeTicket(string ticket);
}
