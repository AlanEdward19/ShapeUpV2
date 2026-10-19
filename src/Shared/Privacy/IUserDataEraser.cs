namespace ShapeUp.Shared.Privacy;

/// <summary>
/// One module's part of account deletion (App Store 5.1.1(v), LGPD): removes or anonymizes everything the module
/// keeps about a user. Implementations are idempotent (a retry after a partial failure finishes the job) and
/// never delete other people's data: records the user wrote for someone else are anonymized or removed only when they are the user's own content.
/// </summary>
public interface IUserDataEraser
{
    string Module { get; }

    /// <summary>Lower value runs first. Authorization (the user row itself) runs last.</summary>
    int Order => 100;

    Task EraseAsync(int userId, string? email, CancellationToken cancellationToken);
}
