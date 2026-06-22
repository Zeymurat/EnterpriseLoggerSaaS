using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Infrastructure.Persistence;

public static class PlatformAdminSeeder
{
    public const string TestEmail = "platform-admin@test.com";
    public const string TestPassword = "PlatformAdmin123!";

    public static async Task SeedTestingAsync(
        ApplicationDbContext context,
        IPasswordHasher hasher,
        CancellationToken cancellationToken = default)
    {
        if (await context.PlatformAdmins.AnyAsync(cancellationToken))
            return;

        var email = Environment.GetEnvironmentVariable("PLATFORM_ADMIN_EMAIL") ?? TestEmail;
        var password = Environment.GetEnvironmentVariable("PLATFORM_ADMIN_PASSWORD") ?? TestPassword;

        await CreateAdminAsync(context, hasher, email, password, cancellationToken);
    }

    public static async Task SeedDevelopmentAsync(
        ApplicationDbContext context,
        IPasswordHasher hasher,
        CancellationToken cancellationToken = default)
    {
        if (await context.PlatformAdmins.AnyAsync(cancellationToken))
            return;

        var email = Environment.GetEnvironmentVariable("PLATFORM_ADMIN_EMAIL");
        var password = Environment.GetEnvironmentVariable("PLATFORM_ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        await CreateAdminAsync(context, hasher, email, password, cancellationToken);
    }

    private static async Task CreateAdminAsync(
        ApplicationDbContext context,
        IPasswordHasher hasher,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        context.PlatformAdmins.Add(new PlatformAdmin
        {
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = hasher.Hash(password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync(cancellationToken);
    }
}
