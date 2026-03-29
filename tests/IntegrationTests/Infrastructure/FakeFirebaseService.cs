using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace IntegrationTests.Infrastructure;

public class FakeFirebaseService : IFirebaseService
{
    public Task<Result<FirebaseTokenData>> VerifyTokenAsync(string idToken, CancellationToken cancellationToken)
    {
        var uid = string.IsNullOrWhiteSpace(idToken) ? "it-user" : idToken;
        return Task.FromResult(Result<FirebaseTokenData>.Success(new FirebaseTokenData(uid, $"{uid}@shapeup.local", "Integration User")));
    }

    public Task<Result> SetCustomClaimsAsync(string firebaseUid, Dictionary<string, object> claims, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success());

    public Task<Result<Dictionary<string, object>>> GetCustomClaimsAsync(string firebaseUid, CancellationToken cancellationToken) =>
        Task.FromResult(Result<Dictionary<string, object>>.Success(new Dictionary<string, object>()));

    public Task<Result> RevokeRefreshTokensAsync(string firebaseUid, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success());
}

