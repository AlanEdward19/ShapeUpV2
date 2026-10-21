namespace ShapeUp.Features.GymManagement.Shared.Abstractions;

using Entities;

public interface IUserPlatformRoleRepository
{
    Task<IReadOnlyList<UserPlatformRole>> GetByUserIdAsync(int userId, CancellationToken cancellationToken);
    Task<UserPlatformRole?> GetByUserIdAndRoleAsync(int userId, PlatformRoleType role, CancellationToken cancellationToken);
    Task AddAsync(UserPlatformRole role, CancellationToken cancellationToken);
    Task UpdateAsync(UserPlatformRole role, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);

    /// <summary>Keyset page of every platform role assignment, ascending Id, with the tier included.</summary>
    Task<IReadOnlyList<UserPlatformRole>> ListKeysetAsync(int? afterId, int take, CancellationToken cancellationToken);
}

