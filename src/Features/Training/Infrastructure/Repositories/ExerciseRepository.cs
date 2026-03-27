namespace ShapeUp.Features.Training.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Data;
using Shared.Abstractions;
using Shared.Entities;
using Shared.Enums;

public class ExerciseRepository(TrainingDbContext dbContext) : IExerciseRepository
{
    private static IQueryable<Exercise> WithFullIncludes(IQueryable<Exercise> query) =>
        query
            .Include(x => x.MuscleProfiles)
            .Include(x => x.Steps)
            .Include(x => x.ExerciseEquipments).ThenInclude(x => x.Equipment);

    public async Task<Exercise?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await WithFullIncludes(dbContext.Exercises)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Exercise>> GetKeysetPageAsync(int? lastId, int pageSize, CancellationToken cancellationToken)
    {
        var query = WithFullIncludes(dbContext.Exercises.AsNoTracking())
            .OrderByDescending(x => x.Id)
            .AsQueryable();

        if (lastId.HasValue)
            query = query.Where(x => x.Id < lastId.Value);

        return await query.Take(pageSize).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Exercise>> SuggestAsync(string name, EMuscleGroup[] muscleGroups, int[] equipmentIds, int limit, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim().ToLowerInvariant();

        var query = WithFullIncludes(dbContext.Exercises.AsNoTracking())
            .Where(x => x.Name.ToLower().Contains(normalizedName)
                        || x.NamePt.ToLower().Contains(normalizedName));

        if (muscleGroups.Length > 0)
            query = query.Where(x => x.MuscleProfiles.Any(m => muscleGroups.Contains(m.MuscleGroup)));

        if (equipmentIds.Length > 0)
            query = query.Where(x => x.ExerciseEquipments.Any(e => equipmentIds.Contains(e.EquipmentId)));

        return await query.OrderBy(x => x.Name).Take(limit).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Exercise exercise, CancellationToken cancellationToken)
    {
        dbContext.Exercises.Add(exercise);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Exercise exercise, CancellationToken cancellationToken)
    {
        dbContext.Exercises.Update(exercise);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Exercise exercise, CancellationToken cancellationToken)
    {
        dbContext.Exercises.Remove(exercise);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
