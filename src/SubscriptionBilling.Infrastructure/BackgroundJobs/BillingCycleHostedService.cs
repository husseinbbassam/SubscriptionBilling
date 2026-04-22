using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SubscriptionBilling.Application.Billing;

namespace SubscriptionBilling.Infrastructure.BackgroundJobs;

/// <summary>
/// Background service that periodically invokes RunBillingCycleCommand. The
/// interval is short for demo purposes — in production this is typically
/// a daily/hourly cron or a scheduled job in your infrastructure.
/// </summary>
public sealed class BillingCycleHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceProvider _services;
    private readonly ILogger<BillingCycleHostedService> _logger;

    public BillingCycleHostedService(IServiceProvider services, ILogger<BillingCycleHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var result = await mediator.Send(new RunBillingCycleCommand(), stoppingToken);
                if (result.InvoicesGenerated > 0)
                {
                    _logger.LogInformation(
                        "Billing cycle ran. Generated {Count} invoices.",
                        result.InvoicesGenerated);
                }
            }
            catch (OperationCanceledException)
            {
                // shutting down
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Billing cycle failed");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
