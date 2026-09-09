namespace ShapeUp.Configurations;

using MassTransit;
using Microsoft.Extensions.Logging;

public sealed class MessagingReceiveFaultLogger(ILogger<MessagingReceiveFaultLogger> logger) : IReceiveObserver
{
    public Task PreReceive(ReceiveContext context) => Task.CompletedTask;

    public Task PostReceive(ReceiveContext context) => Task.CompletedTask;

    public Task PostConsume<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType) where T : class
        => Task.CompletedTask;

    public Task ConsumeFault<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
        where T : class
    {
        logger.LogError(
            exception,
            "Message consume fault for dead-letter routing. MessageId={MessageId}, ConsumerType={ConsumerType}, Reason={Reason}",
            context.MessageId,
            consumerType,
            exception.Message);

        return Task.CompletedTask;
    }

    public Task ReceiveFault(ReceiveContext context, Exception exception)
    {
        logger.LogError(
            exception,
            "Message receive fault for dead-letter routing. MessageId={MessageId}, InputAddress={InputAddress}, Reason={Reason}",
            context.GetMessageId(),
            context.InputAddress,
            exception.Message);

        return Task.CompletedTask;
    }
}
