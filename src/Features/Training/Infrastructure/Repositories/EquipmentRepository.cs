namespace ShapeUp.Features.Training.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Data;
using Shared.Abstractions;
using Shared.Entities;

public class EquipmentRepository(TrainingDbContext dbContext) : IEquipmentRepository
{
    public async Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await dbContext.Equipments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Equipment>> GetKeysetPageAsync(int? lastId, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.Equipments.AsNoTracking().OrderByDescending(x => x.Id).AsQueryable();
        if (lastId.HasValue)
            query = query.Where(x => x.Id < lastId.Value);

        return await query.Take(pageSize).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Equipment equipment, CancellationToken cancellationToken)
    {
        dbContext.Equipments.Add(equipment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken)
    {
        dbContext.Equipments.Update(equipment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Equipment equipment, CancellationToken cancellationToken)
    {
        dbContext.Equipments.Remove(equipment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

