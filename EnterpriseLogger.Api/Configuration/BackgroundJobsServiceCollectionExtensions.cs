using EnterpriseLogger.Api.BackgroundServices;
using EnterpriseLogger.Infrastructure.Notifications;
using EnterpriseLogger.Infrastructure.Retention;

namespace EnterpriseLogger.Api.Configuration;

public static class BackgroundJobsServiceCollectionExtensions
{
    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddScoped<LogRetentionProcessor>();
        services.AddScoped<BillingNotificationProcessor>();

        if (!environment.IsEnvironment("Testing"))
        {
            services.AddHostedService<LogRetentionHostedService>();
            services.AddHostedService<BillingNotificationHostedService>();
        }

        return services;
    }
}
