using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentTenantProvider _tenantProvider;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentTenantProvider tenantProvider) : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasIndex(t => t.ApiKey).IsUnique();
        });

        modelBuilder.Entity<TenantSubscription>(entity =>
        {
            entity.HasOne(s => s.Tenant)
                  .WithMany(t => t.Subscriptions)
                  .HasForeignKey(s => s.TenantId);

            entity.HasOne(s => s.Package)
                  .WithMany()
                  .HasForeignKey(s => s.PackageId);
        });

        modelBuilder.Entity<SystemLog>(entity =>
        {
            entity.ToTable("Logs");

            entity.HasOne(s => s.Tenant)
                  .WithMany(t => t.Logs)
                  .HasForeignKey(s => s.TenantId);

            // Multi-tenant izolasyon: okuma sorgularında yalnızca aktif tenant'ın logları.
            // TenantId çözülmemişse hiçbir log satırı dönmez (sızıntı önlemi).
            entity.HasQueryFilter(log =>
                _tenantProvider.TenantId != null && log.TenantId == _tenantProvider.TenantId);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }
}
