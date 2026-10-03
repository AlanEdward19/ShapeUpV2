namespace ShapeUp.Features.Credentials.ExpireCredentials;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>Runs <see cref="ExpireCredentialsHandler"/> shortly after startup and then every hour.</summary>
public sealed class ExpireCredentialsHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<ExpireCredentialsHostedService> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<ExpireCredentialsHandler>();
                var expired = await handler.HandleAsync(DateTime.UtcNow, stoppingToken);
                if (expired > 0)
                    logger.LogInformation("Expired {Count} professional credentials", expired);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to expire professional credentials; will retry on the next run");
            }
        } while (await WaitNextTickAsync(timer, stoppingToken));
    }

    private static async Task<bool> WaitNextTickAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try { return await timer.WaitForNextTickAsync(cancellationToken); }
        catch (OperationCanceledException) { return false; }
    }
}
