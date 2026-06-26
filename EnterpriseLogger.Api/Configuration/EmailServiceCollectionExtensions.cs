using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Settings;
using EnterpriseLogger.Infrastructure.Email;

namespace EnterpriseLogger.Api.Configuration;

public static class EmailServiceCollectionExtensions
{
    public static IServiceCollection AddEmailNotifications(this IServiceCollection services)
    {
        var enabled = string.Equals(
            Environment.GetEnvironmentVariable("EMAIL_NOTIFICATIONS_ENABLED"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        var settings = new EmailSettings
        {
            Enabled = enabled,
            Host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? string.Empty,
            Port = ParseInt(Environment.GetEnvironmentVariable("SMTP_PORT"), 587),
            Username = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? string.Empty,
            Password = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? string.Empty,
            FromAddress = Environment.GetEnvironmentVariable("SMTP_FROM") ?? string.Empty,
            FromName = Environment.GetEnvironmentVariable("SMTP_FROM_NAME") ?? "EnterpriseLogger",
            UseSsl = !string.Equals(
                Environment.GetEnvironmentVariable("SMTP_USE_SSL"),
                "false",
                StringComparison.OrdinalIgnoreCase),
        };

        services.AddSingleton(settings);
        services.AddScoped<IEmailNotificationService, SmtpEmailNotificationService>();

        return services;
    }

    private static int ParseInt(string? value, int defaultValue) =>
        int.TryParse(value, out var parsed) && parsed > 0 ? parsed : defaultValue;
}
