using System.Collections.Concurrent;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace IntegrationTests.Infrastructure;

public sealed class TestFirebaseService : IFirebaseService
{
    private readonly ConcurrentDictionary<string, Dictionary<string, object>> _claimsByUser = new(StringComparer.OrdinalIgnoreCase);

    public Task<Result<FirebaseTokenData>> VerifyTokenAsync(string idToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            return Task.FromResult(Result<FirebaseTokenData>.Failure(
                CommonErrors.Unauthorized("Token is required.")));
        }

        var parts = idToken.Split('|', StringSplitOptions.TrimEntries);
        if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
        {
            return Task.FromResult(Result<FirebaseTokenData>.Failure(
                CommonErrors.Unauthorized("Token format must be '<uid>|<email>'.")));
        }

        _claimsByUser.TryGetValue(parts[0], out var claims);

        return Task.FromResult(Result<FirebaseTokenData>.Success(new FirebaseTokenData(
            parts[0],
            parts[1],
            DisplayName: null,
            CustomClaims: claims)));
    }

    public Task<Result> SetCustomClaimsAsync(string firebaseUid, Dictionary<string, object> claims, CancellationToken cancellationToken)
    {
        _claimsByUser[firebaseUid] = new Dictionary<string, object>(claims);
        return Task.FromResult(Result.Success());
    }

    public Task<Result<Dictionary<string, object>>> GetCustomClaimsAsync(string firebaseUid, CancellationToken cancellationToken)
    {
        var claims = _claimsByUser.GetValueOrDefault(firebaseUid) ?? [];
        return Task.FromResult(Result<Dictionary<string, object>>.Success(new Dictionary<string, object>(claims)));
    }

    public Task<Result> RevokeRefreshTokensAsync(string firebaseUid, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(firebaseUid))
        {
            return Task.FromResult(Result.Failure(
                CommonErrors.Validation("Firebase UID is required for token revocation.")));
        }

        return Task.FromResult(Result.Success());
    }

    public static string CreateToken(string uid, string email) => $"{uid}|{email}";
}
