using EnterpriseLogger.Api.BackgroundServices;
using EnterpriseLogger.Application.Common.Settings;
using EnterpriseLogger.Application.Common.Subscriptions;
using EnterpriseLogger.Infrastructure.Billing;

namespace EnterpriseLogger.Api.Configuration;

public static class BillingServiceCollectionExtensions
{
    public static IServiceCollection AddBilling(this IServiceCollection services, IHostEnvironment environment)
    {
        var graceDays = ParseGraceDays(Environment.GetEnvironmentVariable("SUBSCRIPTION_PAYMENT_GRACE_DAYS"), 7);
        services.AddSingleton(new BillingSettings { PaymentGraceDays = graceDays });

        services.AddScoped<SubscriptionLifecycleService>();
        services.AddScoped<SubscriptionRenewalProcessor>();

        if (!environment.IsEnvironment("Testing"))
            services.AddHostedService<SubscriptionRenewalHostedService>();

        return services;
    }

    private static int ParseGraceDays(string? value, int defaultValue) =>
        int.TryParse(value, out var days) && days > 0 ? days : defaultValue;
}
