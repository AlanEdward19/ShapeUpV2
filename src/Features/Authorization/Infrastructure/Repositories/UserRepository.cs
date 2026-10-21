namespace ShapeUp.Features.Authorization.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Shared.Abstractions;
using Shared.Data;
using Shared.Entities;

public class UserRepository(AuthorizationDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken) =>
        await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

    public async Task<User?> GetByFirebaseUidAsync(string firebaseUid, CancellationToken cancellationToken) =>
        await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid, cancellationToken);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyCollection<int> userIds, CancellationToken cancellationToken) =>
        userIds.Count == 0
            ? []
            : await context.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken) =>
        await context.Users
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<User>> SearchKeysetAsync(
        string? search, int? afterId, int take, CancellationToken cancellationToken)
    {
        var query = context.Users.AsNoTracking().AsQueryable();
        if (afterId is int id)
            query = query.Where(u => u.Id > id);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                u.Email.Contains(term) || (u.DisplayName != null && u.DisplayName.Contains(term)));
        }

        return await query.OrderBy(u => u.Id).Take(take).ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(bool? isActive, CancellationToken cancellationToken)
    {
        var query = context.Users.AsQueryable();
        if (isActive is bool active)
            query = query.Where(u => u.IsActive == active);
        return query.CountAsync(cancellationToken);
    }

    public async Task<bool> SetActiveAsync(int userId, bool isActive, CancellationToken cancellationToken) =>
        await context.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(u => u.IsActive, isActive)
                    .SetProperty(u => u.UpdatedAt, DateTime.UtcNow),
                cancellationToken) == 1;

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await context.Users.AddAsync(user, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
        user.UpdatedAt = DateTime.UtcNow;
        context.Users.Update(user);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryRegisterMobileExperienceAsync(int userId, DateTime stamp, CancellationToken cancellationToken) =>
        await context.Users.Where(u => u.Id == userId && u.IsActive && u.FirstMobileSeenAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.FirstMobileSeenAtUtc, stamp), cancellationToken) == 1;

    public async Task DeleteAsync(int userId, CancellationToken cancellationToken)
    {
        await context.Users
            .Where(u => u.Id == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}

