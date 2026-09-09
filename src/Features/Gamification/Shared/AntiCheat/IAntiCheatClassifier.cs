using ShapeUp.Features.Training.Shared.Documents;

namespace ShapeUp.Features.Gamification.Shared.AntiCheat;

public interface IAntiCheatClassifier
{
    Task<AntiCheatResult> ClassifyAsync(
        WorkoutSessionDocument session,
        IReadOnlyList<WorkoutSessionDocument> recentSessions,
        CancellationToken cancellationToken = default);
}
