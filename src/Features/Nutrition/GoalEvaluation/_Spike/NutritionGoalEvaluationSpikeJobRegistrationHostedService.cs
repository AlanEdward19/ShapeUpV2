namespace ShapeUp.Features.Nutrition.GoalEvaluation._Spike;

using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public sealed class NutritionGoalEvaluationSpikeJobRegistrationHostedService(
    IBus bus,
    ILogger<NutritionGoalEvaluationSpikeJobRegistrationHostedService> logger) : IHostedService
{
    public const string RecurringJobName = "NutritionGoalEvaluationSpike";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (bus is not IPublishEndpoint publishEndpoint)
        {
            logger.LogWarning("IBus does not implement IPublishEndpoint; skipping spike recurring job registration");
            return;
        }

        await publishEndpoint.AddOrUpdateRecurringJob(
            RecurringJobName,
            new NutritionGoalEvaluationSpikeJob(),
            schedule => schedule.Every(minutes: 2),
            cancellationToken);

        await publishEndpoint.RunRecurringJob<NutritionGoalEvaluationSpikeJob>(RecurringJobName);

        logger.LogInformation(
            "Registered MassTransit recurring spike job {JobName} (every 2 minutes) and triggered initial run",
            RecurringJobName);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
