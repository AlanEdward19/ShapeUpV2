namespace ShapeUp.Features.Authorization.Shared.Abstractions;

using ShapeUp.Shared.Results;

/// <summary>
/// Service for integrating with Firebase Authentication and Custom Claims.
/// </summary>
public interface IFirebaseService
{
    /// <summary>
    /// Verifies a Firebase ID token and extracts claims.
    /// </summary>
    Task<Result<FirebaseTokenData>> VerifyTokenAsync(string idToken, CancellationToken cancellationToken);

    /// <summary>
    /// Updates custom claims for a user in Firebase.
    /// </summary>
    Task<Result> SetCustomClaimsAsync(string firebaseUid, Dictionary<string, object> claims, CancellationToken cancellationToken);

    /// <summary>
    /// Gets custom claims for a user from Firebase.
    /// </summary>
    Task<Result<Dictionary<string, object>>> GetCustomClaimsAsync(string firebaseUid, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes Firebase refresh tokens for the user, invalidating subsequent ID token refreshes.
    /// </summary>
    Task<Result> RevokeRefreshTokensAsync(string firebaseUid, CancellationToken cancellationToken);
}

public record FirebaseTokenData(
    string Uid,
    string Email,
    string? DisplayName,
    Dictionary<string, object>? CustomClaims = null
);
