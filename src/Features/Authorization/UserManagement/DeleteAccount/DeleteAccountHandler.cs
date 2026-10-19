namespace ShapeUp.Features.Authorization.UserManagement.DeleteAccount;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Abstractions;
using Shared.Data;
using ShapeUp.Features.GymManagement.Privacy;
using ShapeUp.Shared.Privacy;
using ShapeUp.Shared.Results;

public record DeleteAccountResponse(DateTime DeletedAtUtc);

/// <summary>
/// Deletes the signed-in user's account for real (App Store 5.1.1(v), LGPD): every module erases its data, then the user row,
/// then the Firebase identity. Each step is idempotent, so after a partial failure the same call finishes the job; until the
/// user row is gone the account still exists and can retry. The Firebase user goes last so the token keeps working for a retry.
/// </summary>
public class DeleteAccountHandler(
    IUserRepository userRepository,
    AuthorizationDbContext authorizationDb,
    IEnumerable<IUserDataEraser> erasers,
    IFirebaseService firebaseService,
    ILogger<DeleteAccountHandler>? logger = null)
{
    public async Task<Result<DeleteAccountResponse>> HandleAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result<DeleteAccountResponse>.Failure(CommonErrors.NotFound("User not found."));

        foreach (var eraser in erasers.OrderBy(e => e.Order))
        {
            try
            {
                await eraser.EraseAsync(userId, user.Email, cancellationToken);
            }
            catch (AccountDeletionBlockedException ex)
            {
                return Result<DeleteAccountResponse>.Failure(CommonErrors.Conflict(ex.Message));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Account deletion of user {UserId} stopped at module {Module}", userId, eraser.Module);
                return Result<DeleteAccountResponse>.Failure(new Error(
                    "account_deletion_incomplete",
                    "Your account could not be fully deleted. Nothing else was lost: try again in a moment.",
                    StatusCodes.Status503ServiceUnavailable));
            }
        }

        await authorizationDb.Users.Where(u => u.Id == userId).ExecuteDeleteAsync(cancellationToken);

        // The data and the API user are gone; a leftover Firebase identity could only create a fresh, empty account.
        var identity = await firebaseService.DeleteUserAsync(user.FirebaseUid, cancellationToken);
        if (identity.IsFailure)
            logger?.LogError("User {UserId} data was deleted but the Firebase identity could not be: {Error}", userId, identity.Error?.Message);

        return Result<DeleteAccountResponse>.Success(new DeleteAccountResponse(DateTime.UtcNow));
    }
}
