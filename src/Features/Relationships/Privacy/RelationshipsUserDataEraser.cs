namespace ShapeUp.Features.Relationships.Privacy;

using Microsoft.EntityFrameworkCore;
using Shared.Data;
using ShapeUp.Shared.Privacy;

public class RelationshipsUserDataEraser(RelationshipsDbContext db) : IUserDataEraser
{
    public string Module => "Relationships";

    public async Task EraseAsync(int userId, string? email, CancellationToken cancellationToken)
    {
        await db.ProfessionalClientRelationships
            .Where(r => r.ProfessionalUserId == userId || r.ClientUserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.ProfessionalClientInvites.Where(i => i.ProfessionalUserId == userId).ExecuteDeleteAsync(cancellationToken);
        // An invite the user accepted keeps no trace of who accepted it.
        await db.ProfessionalClientInvites.Where(i => i.AcceptedByUserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.AcceptedByUserId, (int?)null), cancellationToken);
        if (!string.IsNullOrWhiteSpace(email))
        {
            var normalized = email.Trim().ToLowerInvariant();
            await db.ProfessionalClientInvites.Where(i => i.InviteeEmail == normalized)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.InviteeEmail, (string?)null), cancellationToken);
        }
    }
}
