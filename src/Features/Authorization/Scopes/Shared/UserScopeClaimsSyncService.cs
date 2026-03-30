namespace ShapeUp.Features.Authorization.Scopes.Shared;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Shared.Errors;
using ShapeUp.Shared.Results;

public class UserScopeClaimsSyncService(
    IUserRepository userRepository,
    IScopeRepository scopeRepository,
    IFirebaseService firebaseService,
    ILogger<UserScopeClaimsSyncService> logger) : IUserScopeClaimsSyncService
{
    private const int FirebaseCustomClaimsLimitBytes = 1000;
    private static readonly string[] ManagedClaimKeys =
    [
        "scopes",
        "scopeCount",
        "scopesHash",
        "scopesSource",
        "userId"
    ];

    public async Task<Result<UserScopeClaimsSyncResult>> SyncAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return Result<UserScopeClaimsSyncResult>.Failure(AuthorizationErrors.UserNotFound(userId));

        var scopes = await scopeRepository.GetUserScopesAsync(userId, cancellationToken);
        var scopeNames = scopes.Select(s => s.Name).ToArray();

        var existingClaimsResult = await firebaseService.GetCustomClaimsAsync(user.FirebaseUid, cancellationToken);
        if (existingClaimsResult.IsFailure)
            return Result<UserScopeClaimsSyncResult>.Failure(existingClaimsResult.Error!);

        var claims = BuildClaimsWithinBudget(existingClaimsResult.Value!, user.Id, scopeNames);

        var firebaseResult = await firebaseService.SetCustomClaimsAsync(user.FirebaseUid, claims, cancellationToken);
        if (firebaseResult.IsFailure)
            return Result<UserScopeClaimsSyncResult>.Failure(firebaseResult.Error!);

        return Result<UserScopeClaimsSyncResult>.Success(new UserScopeClaimsSyncResult(userId, scopeNames.Length));
    }

    private Dictionary<string, object> BuildClaimsWithinBudget(
        Dictionary<string, object> existingClaims,
        int userId,
        string[] scopeNames)
    {
        var baseClaims = CopyWithoutManagedClaims(existingClaims);

        var fullClaims = new Dictionary<string, object>(baseClaims)
        {
            ["userId"] = userId,
            ["scopes"] = scopeNames
        };

        if (GetSerializedClaimsSizeInBytes(fullClaims) <= FirebaseCustomClaimsLimitBytes)
            return fullClaims;

        var compactClaims = new Dictionary<string, object>(baseClaims)
        {
            ["userId"] = userId,
            ["scopeCount"] = scopeNames.Length,
            ["scopesHash"] = BuildScopesHash(scopeNames),
            ["scopesSource"] = "db"
        };

        if (GetSerializedClaimsSizeInBytes(compactClaims) <= FirebaseCustomClaimsLimitBytes)
        {
            logger.LogWarning(
                "Firebase custom claims exceeded limit with full scopes list. Falling back to compact claims for user {UserId} with {ScopeCount} scopes.",
                userId,
                scopeNames.Length);
            return compactClaims;
        }

        // As a last resort, keep only managed claims to guarantee Firebase accepts the payload.
        var managedOnlyClaims = new Dictionary<string, object>
        {
            ["userId"] = userId,
            ["scopeCount"] = scopeNames.Length,
            ["scopesHash"] = BuildScopesHash(scopeNames),
            ["scopesSource"] = "db"
        };

        logger.LogWarning(
            "Firebase custom claims were still oversized after compact fallback. Clearing non-managed claims for user {UserId}.",
            userId);

        return managedOnlyClaims;
    }

    private static Dictionary<string, object> CopyWithoutManagedClaims(Dictionary<string, object> existingClaims)
    {
        var filtered = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var claim in existingClaims)
        {
            if (ManagedClaimKeys.Contains(claim.Key, StringComparer.Ordinal))
                continue;

            filtered[claim.Key] = claim.Value;
        }

        return filtered;
    }

    private static int GetSerializedClaimsSizeInBytes(Dictionary<string, object> claims)
    {
        return JsonSerializer.SerializeToUtf8Bytes(claims).Length;
    }

    private static string BuildScopesHash(string[] scopeNames)
    {
        var joinedScopes = string.Join("|", scopeNames.OrderBy(x => x, StringComparer.Ordinal));
        var bytes = Encoding.UTF8.GetBytes(joinedScopes);
        var hash = SHA256.HashData(bytes);

        // Keep the claim small while still changing reliably when scope set changes.
        return Convert.ToHexString(hash.AsSpan(0, 8));
    }
}

