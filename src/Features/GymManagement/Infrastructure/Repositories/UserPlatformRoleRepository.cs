namespace ShapeUp.Features.GymManagement.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Data;
using Shared.Abstractions;
using Shared.Entities;

public class UserPlatformRoleRepository(GymManagementDbContext context) : IUserPlatformRoleRepository
{
    public async Task<IReadOnlyList<UserPlatformRole>> GetByUserIdAsync(int userId, CancellationToken cancellationToken) =>
        await context.UserPlatformRoles
            .Include(r => r.PlatformTier)
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .ToListAsync(cancellationToken);

    public async Task<UserPlatformRole?> GetByUserIdAndRoleAsync(int userId, PlatformRoleType role, CancellationToken cancellationToken) =>
        await context.UserPlatformRoles
            .Include(r => r.PlatformTier)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.Role == role, cancellationToken);

    public async Task AddAsync(UserPlatformRole role, CancellationToken cancellationToken)
    {
        context.UserPlatformRoles.Add(role);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserPlatformRole role, CancellationToken cancellationToken)
    {
        context.UserPlatformRoles.Update(role);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken) =>
        await context.UserPlatformRoles.Where(r => r.Id == id).ExecuteDeleteAsync(cancellationToken);
}

