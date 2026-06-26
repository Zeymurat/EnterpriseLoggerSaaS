using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Domain.Entities;
using EnterpriseLogger.Domain.Enums;
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
    public DbSet<User> Users => Set<User>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<PlatformAdmin> PlatformAdmins => Set<PlatformAdmin>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasIndex(t => t.ApiKeyHash)
                .IsUnique()
                .HasFilter("\"ApiKeyHash\" IS NOT NULL");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();
            entity.HasIndex(u => new { u.TenantId, u.Phone }).IsUnique();

            entity.HasIndex(u => u.TenantId)
                .IsUnique()
                .HasFilter($"\"Role\" = {(int)TenantUserRole.Root}");

            entity.HasOne(u => u.Tenant)
                .WithMany(t => t.Users)
                .HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(u => u.Email).HasMaxLength(256);
            entity.Property(u => u.Phone).HasMaxLength(20);
            entity.Property(u => u.PasswordHash).HasMaxLength(512);
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasIndex(p => p.Code).IsUnique();
            entity.Property(p => p.Code).HasMaxLength(64);
            entity.Property(p => p.Description).HasMaxLength(256);
            entity.HasData(PermissionSeed.GetPermissions());
        });

        modelBuilder.Entity<Package>(entity =>
        {
            entity.HasIndex(p => p.Code).IsUnique();
            entity.Property(p => p.Code).HasMaxLength(32);
            entity.Property(p => p.Name).HasMaxLength(100);
            entity.Property(p => p.Description).HasMaxLength(500);
            entity.Property(p => p.AllowedLogLevels).HasMaxLength(128);
            entity.HasData(PackageSeed.GetDefaultPackages());
        });

        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.HasKey(up => new { up.UserId, up.PermissionId });

            entity.HasOne(up => up.User)
                .WithMany(u => u.UserPermissions)
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(up => up.Permission)
                .WithMany(p => p.UserPermissions)
                .HasForeignKey(up => up.PermissionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PlatformAdmin>(entity =>
        {
            entity.HasIndex(a => a.Email).IsUnique();
            entity.Property(a => a.Email).HasMaxLength(256);
            entity.Property(a => a.PasswordHash).HasMaxLength(512);
        });

        modelBuilder.Entity<TenantSubscription>(entity =>
        {
            entity.HasOne(s => s.Tenant)
                  .WithMany(t => t.Subscriptions)
                  .HasForeignKey(s => s.TenantId);

            entity.HasOne(s => s.Package)
                  .WithMany(p => p.Subscriptions)
                  .HasForeignKey(s => s.PackageId);

            entity.HasIndex(s => new { s.TenantId, s.StartDate });

            entity.HasOne(s => s.Payment)
                .WithOne(p => p.Subscription)
                .HasForeignKey<TenantSubscription>(s => s.PaymentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(s => s.PaymentId)
                .IsUnique()
                .HasFilter("\"PaymentId\" IS NOT NULL");

            entity.Property(s => s.CancellationReason).HasMaxLength(500);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(p => p.Currency).HasMaxLength(8);
            entity.Property(p => p.Method).HasMaxLength(32);
            entity.Property(p => p.ReferenceNumber).HasMaxLength(128);
            entity.Property(p => p.Notes).HasMaxLength(1000);

            entity.HasOne(p => p.Tenant)
                .WithMany()
                .HasForeignKey(p => p.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Package)
                .WithMany()
                .HasForeignKey(p => p.PackageId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(p => new { p.TenantId, p.Status, p.CreatedAt });
            entity.HasIndex(p => p.ReferenceNumber);
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

            entity.HasIndex(log => new { log.TenantId, log.LogLevel, log.Timestamp });
            entity.HasIndex(log => new { log.TenantId, log.CorrelationId });

            entity.Property(l => l.HttpMethod).HasMaxLength(10);
            entity.Property(l => l.RequestPath).HasMaxLength(500);
            entity.Property(l => l.CorrelationId).HasMaxLength(64);
            entity.Property(l => l.ActorIdentifier).HasMaxLength(256);
            entity.Property(l => l.ExceptionType).HasMaxLength(256);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }
}
