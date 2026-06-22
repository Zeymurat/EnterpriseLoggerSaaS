using EnterpriseLogger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (!await context.Permissions.AnyAsync(cancellationToken))
        {
            context.Permissions.AddRange(PermissionSeed.GetPermissions());
            await context.SaveChangesAsync(cancellationToken);
        }

        if (!await context.Packages.AnyAsync(cancellationToken))
        {
            context.Packages.AddRange(PackageSeed.GetDefaultPackages());
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
