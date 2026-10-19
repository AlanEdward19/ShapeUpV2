namespace ShapeUp.Features.Credentials.ReverifyCredentials;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>Runs <see cref="ReverifyCredentialsHandler"/> a few minutes after startup and then every 6 hours. Does nothing while no council is enabled.</summary>
public sealed class ReverifyCredentialsHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<ReverifyCredentialsHostedService> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<ReverifyCredentialsHandler>();
                var suspended = await handler.HandleAsync(DateTime.UtcNow, stoppingToken);
                if (suspended > 0)
                    logger.LogInformation("Suspended {Count} professional credentials after council reverification", suspended);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Council reverification failed; will retry on the next run");
            }
        } while (await WaitNextTickAsync(timer, stoppingToken));
    }

    private static async Task<bool> WaitNextTickAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try { return await timer.WaitForNextTickAsync(cancellationToken); }
        catch (OperationCanceledException) { return false; }
    }
}
