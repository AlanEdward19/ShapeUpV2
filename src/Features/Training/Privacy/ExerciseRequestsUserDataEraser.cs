namespace ShapeUp.Features.Training.Privacy;

using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Shared.Entities;
using ShapeUp.Shared.Privacy;

/// <summary>
/// Open and rejected exercise requests of the user are deleted. Approved ones stay (the exercise is linked to them) but
/// lose the requester and the free text; reviews done by the user keep the trail without the reviewer id.
/// </summary>
public class ExerciseRequestsUserDataEraser(TrainingDbContext dbContext) : IUserDataEraser
{
    public string Module => "TrainingExerciseRequests";

    public async Task EraseAsync(int userId, string? email, CancellationToken cancellationToken)
    {
        await dbContext.ExerciseRequests
            .Where(r => r.RequestedByUserId == userId && r.Status != ExerciseRequest.StatusApproved)
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.ExerciseRequests
            .Where(r => r.RequestedByUserId == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.RequestedByUserId, 0)
                .SetProperty(r => r.Notes, (string?)null)
                .SetProperty(r => r.ReferenceUrl, (string?)null)
                .SetProperty(r => r.EquipmentOther, (string?)null), cancellationToken);

        await dbContext.ExerciseRequests
            .Where(r => r.ReviewedByUserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.ReviewedByUserId, (int?)null), cancellationToken);
    }
}
