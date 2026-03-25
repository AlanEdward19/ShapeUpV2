namespace ShapeUp.Features.GymManagement.Shared.Abstractions;

using ShapeUp.Features.GymManagement.Shared.Entities;

public interface IUserPlatformRoleRepository
{
    Task<IReadOnlyList<UserPlatformRole>> GetByUserIdAsync(int userId, CancellationToken cancellationToken);
    Task<UserPlatformRole?> GetByUserIdAndRoleAsync(int userId, PlatformRoleType role, CancellationToken cancellationToken);
    Task AddAsync(UserPlatformRole role, CancellationToken cancellationToken);
    Task UpdateAsync(UserPlatformRole role, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}

