using EnterpriseLogger.Infrastructure.Billing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnterpriseLogger.Api.BackgroundServices;

public class SubscriptionRenewalHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionRenewalHostedService> _logger;

    public SubscriptionRenewalHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<SubscriptionRenewalHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessCycleAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscription renewal cycle failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<SubscriptionRenewalProcessor>();
        await processor.ProcessAsync(cancellationToken);
    }
}
