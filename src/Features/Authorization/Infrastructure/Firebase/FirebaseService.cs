namespace ShapeUp.Features.Authorization.Infrastructure.Firebase;

using FirebaseAdmin.Auth;
using Shared.Abstractions;
using ShapeUp.Shared.Results;

public class FirebaseService(FirebaseAuth firebaseAuth) : IFirebaseService
{
    public async Task<Result<FirebaseTokenData>> VerifyTokenAsync(string idToken, CancellationToken cancellationToken)
    {
        try
        {
            var decodedToken = await firebaseAuth.VerifyIdTokenAsync(idToken, cancellationToken);

            var customClaims = decodedToken.Claims
                .Where(c => !c.Key.StartsWith("firebase_"))
                .Where(c => !c.Key.StartsWith("iss") && !c.Key.StartsWith("aud") && !c.Key.StartsWith("auth_time"))
                .Where(c => c.Key != "user_id" && c.Key != "sub" && c.Key != "email_verified")
                .ToDictionary(c => c.Key, c => c.Value);

            var tokenData = new FirebaseTokenData(
                Uid: decodedToken.Uid,
                Email: decodedToken.Claims.TryGetValue("email", out var email) ? email.ToString()! : string.Empty,
                DisplayName: decodedToken.Claims.TryGetValue("name", out var name) ? name.ToString() : null,
                CustomClaims: customClaims
            );

            return Result<FirebaseTokenData>.Success(tokenData);
        }
        catch (FirebaseAuthException ex)
        {
            return Result<FirebaseTokenData>.Failure(CommonErrors.Unauthorized($"Invalid or expired token: {ex.Message}"));
        }
    }

    public async Task<Result> SetCustomClaimsAsync(string firebaseUid, Dictionary<string, object> claims, CancellationToken cancellationToken)
    {
        try
        {
            await firebaseAuth.SetCustomUserClaimsAsync(firebaseUid, claims, cancellationToken);
            return Result.Success();
        }
        catch (FirebaseAuthException ex)
        {
            return Result.Failure(new Error(
                "firebase_claims_update_failed",
                $"Failed to update Firebase custom claims for user {firebaseUid}: {ex.Message}",
                StatusCodes.Status500InternalServerError));
        }
    }

    public async Task<Result<Dictionary<string, object>>> GetCustomClaimsAsync(string firebaseUid, CancellationToken cancellationToken)
    {
        try
        {
            var user = await firebaseAuth.GetUserAsync(firebaseUid, cancellationToken);
            var claims = user.CustomClaims?.ToDictionary(c => c.Key, c => c.Value) ?? [];
            return Result<Dictionary<string, object>>.Success(claims);
        }
        catch (FirebaseAuthException ex)
        {
            return Result<Dictionary<string, object>>.Failure(new Error(
                "firebase_user_lookup_failed",
                $"Failed to retrieve Firebase user {firebaseUid}: {ex.Message}",
                StatusCodes.Status500InternalServerError));
        }
    }

    public async Task<Result> RevokeRefreshTokensAsync(string firebaseUid, CancellationToken cancellationToken)
    {
        try
        {
            await firebaseAuth.RevokeRefreshTokensAsync(firebaseUid, cancellationToken);
            return Result.Success();
        }
        catch (FirebaseAuthException ex)
        {
            return Result.Failure(new Error(
                "firebase_token_revocation_failed",
                $"Failed to revoke Firebase tokens for user {firebaseUid}: {ex.Message}",
                StatusCodes.Status500InternalServerError));
        }
    }
}
