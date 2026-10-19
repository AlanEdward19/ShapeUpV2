namespace ShapeUp.Features.GymManagement.Privacy;

using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Shared.Privacy;
using ShapeUp.Shared.Results;

/// <summary>Raised when the user owns a gym that other people still belong to: deleting the account would delete theirs too.</summary>
public sealed class AccountDeletionBlockedException(string message) : Exception(message);

public class GymManagementUserDataEraser(GymManagementDbContext db) : IUserDataEraser
{
    public string Module => "GymManagement";

    public async Task EraseAsync(int userId, string? email, CancellationToken cancellationToken)
    {
        // A gym the user owns goes with them only when nobody else is in it.
        var ownedGymIds = await db.Gyms.Where(g => g.OwnerId == userId).Select(g => g.Id).ToListAsync(cancellationToken);
        if (ownedGymIds.Count > 0)
        {
            var othersInside = await db.GymClients.AnyAsync(c => ownedGymIds.Contains(c.GymId) && c.UserId != userId, cancellationToken)
                               || await db.GymStaff.AnyAsync(s => ownedGymIds.Contains(s.GymId) && s.UserId != userId, cancellationToken);
            if (othersInside)
                throw new AccountDeletionBlockedException(
                    "You own a gym that still has members or staff. Remove them or hand the gym over before deleting your account.");
        }

        // Clients assigned to this user as a gym trainer lose the assignment (the FK is restrictive).
        var staffIds = await db.GymStaff.Where(s => s.UserId == userId).Select(s => s.Id).ToListAsync(cancellationToken);
        if (staffIds.Count > 0)
            await db.GymClients.Where(c => c.TrainerId != null && staffIds.Contains(c.TrainerId.Value))
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.TrainerId, (int?)null), cancellationToken);

        await db.GymClients.Where(c => c.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.GymStaff.Where(s => s.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        if (ownedGymIds.Count > 0)
            await db.Gyms.Where(g => ownedGymIds.Contains(g.Id)).ExecuteDeleteAsync(cancellationToken);

        await db.TrainerClients.Where(t => t.TrainerId == userId || t.ClientId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.TrainerClientInvites.Where(i => i.TrainerId == userId).ExecuteDeleteAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(email))
        {
            var normalized = email.Trim().ToLowerInvariant();
            await db.TrainerClientInvites.Where(i => i.InviteeEmail == normalized).ExecuteDeleteAsync(cancellationToken);
        }

        await db.TrainerPlans.Where(p => p.TrainerId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.UserPlatformRoles.Where(r => r.UserId == userId).ExecuteDeleteAsync(cancellationToken);
    }
}
