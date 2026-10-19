namespace ShapeUp.Features.Credentials.Privacy;

using Microsoft.EntityFrameworkCore;
using Shared.Data;
using ShapeUp.Shared.Privacy;

public class CredentialsUserDataEraser(CredentialsDbContext db) : IUserDataEraser
{
    public string Module => "Credentials";

    public async Task EraseAsync(int userId, string? email, CancellationToken cancellationToken)
    {
        await db.ProfessionalCredentials.Where(c => c.UserId == userId).ExecuteDeleteAsync(cancellationToken);
    }
}
