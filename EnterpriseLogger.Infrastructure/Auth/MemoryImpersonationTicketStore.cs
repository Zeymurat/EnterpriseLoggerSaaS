using System.Collections.Concurrent;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Features.Auth.Dtos;

namespace EnterpriseLogger.Infrastructure.Auth;

public class MemoryImpersonationTicketStore : IImpersonationTicketStore
{
    private readonly ConcurrentDictionary<string, (LoginResponse Session, DateTime ExpiresAtUtc)> _tickets =
        new();

    public string CreateTicket(LoginResponse session, TimeSpan ttl)
    {
        var ticket = Guid.NewGuid().ToString("N");
        _tickets[ticket] = (session, DateTime.UtcNow.Add(ttl));
        return ticket;
    }

    public LoginResponse? ConsumeTicket(string ticket)
    {
        if (string.IsNullOrWhiteSpace(ticket))
            return null;

        if (!_tickets.TryRemove(ticket.Trim(), out var entry))
            return null;

        if (DateTime.UtcNow > entry.ExpiresAtUtc)
            return null;

        return entry.Session;
    }
}
