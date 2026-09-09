namespace IntegrationTests.Domains.Messaging;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

public sealed class WorkoutFinishedConsumerLogCapture : ILoggerProvider
{
    public static ConcurrentBag<string> Entries { get; } = [];

    public ILogger CreateLogger(string categoryName) => new CaptureLogger(categoryName);

    public void Dispose()
    {
    }

    public static void Reset() => Entries.Clear();

    public static bool ContainsSessionId(string sessionId) =>
        Entries.Any(entry => entry.Contains(sessionId, StringComparison.Ordinal));

    private sealed class CaptureLogger(string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) =>
            logLevel >= LogLevel.Information
            && categoryName.Contains("WorkoutFinishedConsumer", StringComparison.Ordinal);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            Entries.Add(formatter(state, exception));
        }
    }
}
