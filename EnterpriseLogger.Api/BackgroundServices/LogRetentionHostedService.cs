using EnterpriseLogger.Infrastructure.Retention;

namespace EnterpriseLogger.Api.BackgroundServices;

public class LogRetentionHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LogRetentionHostedService> _logger;

    public LogRetentionHostedService(
        IServiceProvider serviceProvider,
        ILogger<LogRetentionHostedService> logger)
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
                var processor = scope.ServiceProvider.GetRequiredService<LogRetentionProcessor>();
                var deleted = await processor.ProcessAsync(stoppingToken);
                if (deleted > 0)
                    _logger.LogInformation("Log retention removed {DeletedCount} rows.", deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Log retention cycle failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
