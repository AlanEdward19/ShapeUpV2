namespace ShapeUp.Features.Gamification.Shared;

public interface IShapeScoreCalculator
{
    Task<int> CalculateAsync(int userId, CancellationToken cancellationToken);
}
