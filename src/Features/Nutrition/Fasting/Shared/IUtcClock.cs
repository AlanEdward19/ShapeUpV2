namespace ShapeUp.Features.Nutrition.Fasting.Shared;

public interface IUtcClock
{
    DateTime UtcNow { get; }
}

public sealed class SystemUtcClock : IUtcClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
