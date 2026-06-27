using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Api.Configuration;

public static class DatabaseMigrationExtensions
{
    public static async Task ApplyMigrationsAndSeedAsync(this WebApplication app)
    {
        if (app.Environment.IsEnvironment("Testing"))
            return;

        var shouldMigrate = app.Environment.IsDevelopment()
            || string.Equals(
                Environment.GetEnvironmentVariable("RUN_DATABASE_MIGRATIONS"),
                "true",
                StringComparison.OrdinalIgnoreCase);

        if (!shouldMigrate)
            return;

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await db.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(db);
        await PlatformAdminSeeder.SeedDevelopmentAsync(db, hasher);
    }
}
