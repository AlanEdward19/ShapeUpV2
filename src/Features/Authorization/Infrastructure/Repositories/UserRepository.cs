namespace ShapeUp.Features.Authorization.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Shared.Abstractions;
using Shared.Data;
using Shared.Entities;

public class UserRepository(AuthorizationDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken) =>
        await context.Users
            .Include(u => u.Groups)
            .Include(u => u.Scopes)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

    public async Task<User?> GetByFirebaseUidAsync(string firebaseUid, CancellationToken cancellationToken) =>
        await context.Users
            .Include(u => u.Groups)
            .Include(u => u.Scopes)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid, cancellationToken);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        await context.Users
            .Include(u => u.Groups)
            .Include(u => u.Scopes)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken) =>
        await context.Users
            .Include(u => u.Groups)
            .Include(u => u.Scopes)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

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

    public async Task DeleteAsync(int userId, CancellationToken cancellationToken)
    {
        await context.Users
            .Where(u => u.Id == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}

