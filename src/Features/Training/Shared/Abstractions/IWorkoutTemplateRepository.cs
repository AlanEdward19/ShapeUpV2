using ShapeUp.Features.Training.Shared.Documents;

namespace ShapeUp.Features.Training.Shared.Abstractions;

public interface IWorkoutTemplateRepository
{
    Task AddAsync(WorkoutTemplateDocument template, CancellationToken cancellationToken);
    Task<WorkoutTemplateDocument?> GetByIdAsync(string templateId, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkoutTemplateDocument>> GetByCreatorKeysetAsync(int creatorUserId, DateTime? createdBeforeUtc, int pageSize, CancellationToken cancellationToken);
}

