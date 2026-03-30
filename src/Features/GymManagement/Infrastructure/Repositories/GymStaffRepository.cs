namespace ShapeUp.Features.GymManagement.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Data;
using Shared.Abstractions;
using Shared.Entities;

public class GymStaffRepository(GymManagementDbContext context) : IGymStaffRepository
{
    public async Task<GymStaff?> GetByIdAsync(int staffId, CancellationToken cancellationToken) =>
        await context.GymStaff.AsNoTracking().FirstOrDefaultAsync(s => s.Id == staffId, cancellationToken);

    public async Task<GymStaff?> GetByGymAndUserAsync(int gymId, int userId, CancellationToken cancellationToken) =>
        await context.GymStaff.AsNoTracking()
            .FirstOrDefaultAsync(s => s.GymId == gymId && s.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<GymStaff>> GetByGymIdKeysetAsync(int gymId, int? lastId, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.GymStaff.AsNoTracking().Where(s => s.GymId == gymId);
        if (lastId.HasValue) query = query.Where(s => s.Id < lastId.Value);
        return await query.OrderByDescending(s => s.Id).Take(pageSize).ToListAsync(cancellationToken);
    }

    public async Task<bool> IsStaffAsync(int gymId, int userId, CancellationToken cancellationToken) =>
        await context.GymStaff.AnyAsync(s => s.GymId == gymId && s.UserId == userId && s.IsActive, cancellationToken);

    public async Task<bool> IsOwnerOrReceptionistAsync(int gymId, int userId, int ownerId, CancellationToken cancellationToken)
    {
        if (userId == ownerId) return true;
        return await context.GymStaff.AnyAsync(
            s => s.GymId == gymId && s.UserId == userId && s.IsActive && s.Role == GymStaffRole.Receptionist,
            cancellationToken);
    }

    public async Task AddAsync(GymStaff staff, CancellationToken cancellationToken)
    {
        await context.GymStaff.AddAsync(staff, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(int staffId, CancellationToken cancellationToken) =>
        await context.GymStaff.Where(s => s.Id == staffId).ExecuteDeleteAsync(cancellationToken);
}

