namespace UnitTests.Support;

/// <summary>A clock that stands still, so a test can put a deadline exactly on, one tick before or one tick after "now".</summary>
public sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
{
    public DateTime UtcNow { get; } = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);

    public override DateTimeOffset GetUtcNow() => new(UtcNow);
}
