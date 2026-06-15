using EnterpriseLogger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.Permissions.AnyAsync(cancellationToken))
            return;

        context.Permissions.AddRange(PermissionSeed.GetPermissions());
        await context.SaveChangesAsync(cancellationToken);
    }
}
