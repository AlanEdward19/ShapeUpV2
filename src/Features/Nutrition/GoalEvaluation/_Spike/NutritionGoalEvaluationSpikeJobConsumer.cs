namespace ShapeUp.Features.Nutrition.GoalEvaluation._Spike;

using MassTransit;
using Microsoft.Extensions.Logging;

// T1 spike — throwaway; replaced by NutritionGoalEvaluationJobConsumer in T18.
// Working MassTransit v9.2.1 APIs (verified in T1):
// - AddConsumer<T>() + IJobConsumer<T>.Run(JobContext<T>)
// - AddDelayedMessageScheduler() + cfg.UseDelayedMessageScheduler()
// - SetInMemorySagaRepositoryProvider() + AddJobSagaStateMachines()
// - IPublishEndpoint.AddOrUpdateRecurringJob(name, message, schedule => schedule.Every(...))
// - IPublishEndpoint.RunRecurringJob<T>(name) for immediate run after registration
// - RabbitMQ requires rabbitmq_delayed_message_exchange plugin (UseDelayedMessageScheduler)
public sealed class NutritionGoalEvaluationSpikeJobConsumer(
    ILogger<NutritionGoalEvaluationSpikeJobConsumer> logger) : IJobConsumer<NutritionGoalEvaluationSpikeJob>
{
    public Task Run(JobContext<NutritionGoalEvaluationSpikeJob> context)
    {
        logger.LogInformation(
            "[T1 SPIKE] NutritionGoalEvaluationSpikeJob executed at {ExecutedAtUtc} (JobId={JobId})",
            DateTime.UtcNow,
            context.JobId);

        return Task.CompletedTask;
    }
}
