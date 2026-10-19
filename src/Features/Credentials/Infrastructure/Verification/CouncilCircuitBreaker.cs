namespace ShapeUp.Features.Credentials.Infrastructure.Verification;

using System.Collections.Concurrent;

/// <summary>Per-council breaker: after N consecutive failures the council is not called for a while.</summary>
public class CouncilCircuitBreaker(TimeProvider? timeProvider = null)
{
    private sealed class State
    {
        public int Failures;
        public DateTime OpenUntilUtc;
    }

    private readonly ConcurrentDictionary<string, State> _states = new();
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;

    public bool IsOpen(string authority)
    {
        if (!_states.TryGetValue(authority, out var state))
            return false;
        lock (state)
            return state.OpenUntilUtc > _time.GetUtcNow().UtcDateTime;
    }

    public void RecordSuccess(string authority)
    {
        if (!_states.TryGetValue(authority, out var state))
            return;
        lock (state)
        {
            state.Failures = 0;
            state.OpenUntilUtc = DateTime.MinValue;
        }
    }

    public void RecordFailure(string authority, int threshold, TimeSpan openFor)
    {
        var state = _states.GetOrAdd(authority, _ => new State());
        lock (state)
        {
            state.Failures++;
            if (state.Failures >= Math.Max(1, threshold))
            {
                state.OpenUntilUtc = _time.GetUtcNow().UtcDateTime.Add(openFor);
                state.Failures = 0;
            }
        }
    }
}
