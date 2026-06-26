using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Auth.Dtos;

namespace EnterpriseLogger.Application.Features.Auth.Commands;

public class ConsumeImpersonationTicketCommand
{
    private readonly IImpersonationTicketStore _ticketStore;

    public ConsumeImpersonationTicketCommand(IImpersonationTicketStore ticketStore)
    {
        _ticketStore = ticketStore;
    }

    public Task<Result<LoginResponse>> ExecuteAsync(
        string ticket,
        CancellationToken cancellationToken = default)
    {
        var session = _ticketStore.ConsumeTicket(ticket);

        if (session is null)
        {
            return Task.FromResult(
                Result<LoginResponse>.Failure("Geçersiz veya süresi dolmuş login-as bağlantısı."));
        }

        return Task.FromResult(Result<LoginResponse>.Success(session));
    }
}
