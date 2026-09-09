namespace IntegrationTests.Domains.Messaging;

using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

public sealed partial class WorkoutFinishedConsumerLogCapture : ILoggerProvider
{
    public static ConcurrentBag<string> Entries { get; } = [];
    public static ConcurrentBag<CapturedWorkoutFinishedLog> Captured { get; } = [];

    public ILogger CreateLogger(string categoryName) => new CaptureLogger(categoryName);

    public void Dispose()
    {
    }

    public static void Reset()
    {
        Entries.Clear();
        Captured.Clear();
    }

    public static bool ContainsSessionId(string sessionId) =>
        Entries.Any(entry => entry.Contains(sessionId, StringComparison.Ordinal));

    public static bool TryGetCaptured(string sessionId, out CapturedWorkoutFinishedLog captured)
    {
        captured = Captured.FirstOrDefault(entry => entry.SessionId == sessionId)!;
        return captured is not null;
    }

    public static bool ContainsExpectedPayload(
        string sessionId,
        int targetUserId,
        int executedByUserId,
        DateTime endedAtUtc)
    {
        if (!TryGetCaptured(sessionId, out var captured))
            return false;

        return captured.TargetUserId == targetUserId
            && captured.ExecutedByUserId == executedByUserId
            && Math.Abs((captured.EndedAtUtc.ToUniversalTime() - endedAtUtc.ToUniversalTime()).TotalSeconds) < 1;
    }

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

            var formatted = formatter(state, exception);
            Entries.Add(formatted);

            var match = LogPattern().Match(formatted);
            if (match.Success)
            {
                Captured.Add(new CapturedWorkoutFinishedLog(
                    match.Groups["sessionId"].Value,
                    int.Parse(match.Groups["targetUserId"].Value, CultureInfo.InvariantCulture),
                    int.Parse(match.Groups["executedByUserId"].Value, CultureInfo.InvariantCulture),
                    DateTime.Parse(match.Groups["endedAtUtc"].Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal)));
            }
        }
    }

    [GeneratedRegex(
        @"Workout finished for session (?<sessionId>.+?) \(target user (?<targetUserId>\d+), executed by (?<executedByUserId>\d+), ended at (?<endedAtUtc>.+?)\)",
        RegexOptions.CultureInvariant)]
    private static partial Regex LogPattern();
}

public sealed record CapturedWorkoutFinishedLog(
    string SessionId,
    int TargetUserId,
    int ExecutedByUserId,
    DateTime EndedAtUtc);
