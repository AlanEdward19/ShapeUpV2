namespace ShapeUp.Features.Nutrition.GoalEvaluation;

using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public sealed class NutritionGoalEvaluationJobRegistrationHostedService(
    IBus bus,
    ILogger<NutritionGoalEvaluationJobRegistrationHostedService> logger) : IHostedService
{
    public const string RecurringJobName = "EvaluateNutritionGoals";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (bus is not IPublishEndpoint publishEndpoint)
        {
            logger.LogWarning("IBus does not implement IPublishEndpoint; skipping nutrition goal recurring job registration");
            return;
        }

        await publishEndpoint.AddOrUpdateRecurringJob(
            RecurringJobName,
            new EvaluateNutritionGoals(),
            "0 5 0 * * *",
            cancellationToken);

        logger.LogInformation(
            "Registered MassTransit recurring nutrition goal evaluation job {JobName} (daily 00:05 UTC)",
            RecurringJobName);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
