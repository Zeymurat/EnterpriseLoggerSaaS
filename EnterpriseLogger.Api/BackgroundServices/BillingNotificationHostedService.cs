using EnterpriseLogger.Infrastructure.Notifications;

namespace EnterpriseLogger.Api.BackgroundServices;

public class BillingNotificationHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BillingNotificationHostedService> _logger;

    public BillingNotificationHostedService(
        IServiceProvider serviceProvider,
        ILogger<BillingNotificationHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<BillingNotificationProcessor>();
                await processor.ProcessAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Billing notification cycle failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
