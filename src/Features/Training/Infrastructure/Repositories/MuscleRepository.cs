namespace ShapeUp.Features.Training.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Data;
using Shared.Abstractions;
using Shared.Entities;

public class MuscleRepository(TrainingDbContext dbContext) : IMuscleRepository
{
    public async Task<Muscle?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await dbContext.Muscles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Muscle>> GetKeysetPageAsync(int? lastId, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.Muscles.AsNoTracking().OrderByDescending(x => x.Id).AsQueryable();
        if (lastId.HasValue)
            query = query.Where(x => x.Id < lastId.Value);

        return await query.Take(pageSize).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Muscle muscle, CancellationToken cancellationToken)
    {
        dbContext.Muscles.Add(muscle);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Muscle muscle, CancellationToken cancellationToken)
    {
        dbContext.Muscles.Update(muscle);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Muscle muscle, CancellationToken cancellationToken)
    {
        dbContext.Muscles.Remove(muscle);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

