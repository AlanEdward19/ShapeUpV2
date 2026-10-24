namespace ShapeUp.Features.Training.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Data;
using Shared.Abstractions;
using Shared.Entities;

public class ExerciseRequestRepository(TrainingDbContext dbContext) : IExerciseRequestRepository
{
    public async Task AddAsync(ExerciseRequest request, CancellationToken cancellationToken)
    {
        dbContext.ExerciseRequests.Add(request);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ExerciseRequest?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await dbContext.ExerciseRequests.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<int> CountPendingByUserAsync(int userId, CancellationToken cancellationToken) =>
        await dbContext.ExerciseRequests.CountAsync(
            x => x.RequestedByUserId == userId && x.Status == ExerciseRequest.StatusPending, cancellationToken);

    public async Task<IReadOnlyList<ExerciseRequest>> GetKeysetPageAsync(
        int? userId, string? status, int? lastId, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.ExerciseRequests.AsNoTracking().AsQueryable();
        if (userId.HasValue)
            query = query.Where(x => x.RequestedByUserId == userId.Value);
        if (status is not null)
            query = query.Where(x => x.Status == status);
        if (lastId.HasValue)
            query = query.Where(x => x.Id < lastId.Value);

        return await query.OrderByDescending(x => x.Id).Take(pageSize).ToListAsync(cancellationToken);
    }

    public async Task SaveAsync(Exercise? newExercise, CancellationToken cancellationToken)
    {
        if (newExercise is not null)
            dbContext.Exercises.Add(newExercise);

        // One SaveChanges is one database transaction: the exercise and the request update commit together or not at all.
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
