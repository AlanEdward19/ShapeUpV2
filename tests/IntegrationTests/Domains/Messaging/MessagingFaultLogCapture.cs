namespace IntegrationTests.Domains.Messaging;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

public sealed class MessagingFaultLogCapture : ILoggerProvider
{
    public static ConcurrentBag<string> Entries { get; } = [];

    public ILogger CreateLogger(string categoryName) => new CaptureLogger(categoryName);

    public void Dispose()
    {
    }

    public static void Reset() => Entries.Clear();

    public static bool ContainsDeadLetterEvidence(Guid messageId, string expectedReasonFragment)
    {
        return Entries.Any(entry =>
            entry.Contains(messageId.ToString(), StringComparison.OrdinalIgnoreCase)
            && entry.Contains(expectedReasonFragment, StringComparison.OrdinalIgnoreCase)
            && (entry.Contains("receive fault", StringComparison.OrdinalIgnoreCase)
                || entry.Contains("consume fault", StringComparison.OrdinalIgnoreCase)));
    }

    private sealed class CaptureLogger(string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) =>
            logLevel >= LogLevel.Error
            && (categoryName.Contains("MessagingReceiveFaultLogger", StringComparison.Ordinal)
                || categoryName.Contains("MassTransit", StringComparison.Ordinal)
                || categoryName.Contains("ShapeUp.Configurations", StringComparison.Ordinal));

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            if (exception is not null)
                message = $"{message} | {exception.GetType().Name}: {exception.Message}";

            Entries.Add(message);
        }
    }
}
