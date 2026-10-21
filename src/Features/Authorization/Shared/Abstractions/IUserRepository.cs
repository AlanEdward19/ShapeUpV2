namespace ShapeUp.Features.Authorization.Shared.Abstractions;

using Entities;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken);
    Task<User?> GetByFirebaseUidAsync(string firebaseUid, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyCollection<int> userIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Keyset page ordered by Id ascending. Search matches email or display name at the database.</summary>
    Task<IReadOnlyList<User>> SearchKeysetAsync(string? search, int? afterId, int take, CancellationToken cancellationToken);

    Task<int> CountAsync(bool? isActive, CancellationToken cancellationToken);

    /// <summary>Sets IsActive. Returns false when the user does not exist.</summary>
    Task<bool> SetActiveAsync(int userId, bool isActive, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);
    Task UpdateAsync(User user, CancellationToken cancellationToken);
    Task<bool> TryRegisterMobileExperienceAsync(int userId, DateTime stamp, CancellationToken cancellationToken);
    Task DeleteAsync(int userId, CancellationToken cancellationToken);
}

