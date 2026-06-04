using EnterpriseLogger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Common.Interfaces;

/// <summary>
/// Application katmanının kalıcılık ile konuştuğu sözleşme.
/// Somut <see cref="DbContext"/> yalnızca Infrastructure'da yaşar.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<SystemLog> SystemLogs { get; }
    DbSet<Package> Packages { get; }
    DbSet<TenantSubscription> TenantSubscriptions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
