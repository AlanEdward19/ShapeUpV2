using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Training.Infrastructure.Data;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Entities;

namespace ShapeUp.Features.Training.Infrastructure.Repositories;

public class ExerciseEquivalentRepository(TrainingDbContext dbContext) : IExerciseEquivalentRepository
{
    public async Task<IReadOnlyList<Exercise>> GetEquivalentsAsync(int exerciseId, CancellationToken cancellationToken)
    {
        var otherIds = await dbContext.ExerciseEquivalents
            .AsNoTracking()
            .Where(x => x.ExerciseId == exerciseId || x.EquivalentExerciseId == exerciseId)
            .Select(x => x.ExerciseId == exerciseId ? x.EquivalentExerciseId : x.ExerciseId)
            .ToListAsync(cancellationToken);

        if (otherIds.Count == 0)
            return [];

        return await dbContext.Exercises
            .AsNoTracking()
            .Include(x => x.MuscleProfiles)
            .Include(x => x.Steps)
            .Include(x => x.ExerciseEquipments).ThenInclude(x => x.Equipment)
            .Where(x => otherIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task SetEquivalentAsync(int exerciseId, int otherExerciseId, CancellationToken cancellationToken)
    {
        var (left, right) = Canonicalize(exerciseId, otherExerciseId);
        var exists = await dbContext.ExerciseEquivalents
            .AnyAsync(x => x.ExerciseId == left && x.EquivalentExerciseId == right, cancellationToken);
        if (exists)
            return;

        dbContext.ExerciseEquivalents.Add(new ExerciseEquivalent
        {
            ExerciseId = left,
            EquivalentExerciseId = right,
            CreatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveEquivalentAsync(int exerciseId, int otherExerciseId, CancellationToken cancellationToken)
    {
        var (left, right) = Canonicalize(exerciseId, otherExerciseId);
        var row = await dbContext.ExerciseEquivalents
            .FirstOrDefaultAsync(x => x.ExerciseId == left && x.EquivalentExerciseId == right, cancellationToken);
        if (row is null)
            return;

        dbContext.ExerciseEquivalents.Remove(row);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static (int Left, int Right) Canonicalize(int a, int b) =>
        a < b ? (a, b) : (b, a);
}
