namespace ShapeUp.Features.PlatformFeatureFlags.GetFeatureFlags;

using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Shared.Results;

public class GetFeatureFlagsHandler(PlatformFeatureFlagsDbContext context)
{
    public async Task<Result<IReadOnlyList<FeatureFlagResponse>>> HandleAsync(CancellationToken cancellationToken)
    {
        var flags = await context.Flags
            .AsNoTracking()
            .OrderBy(f => f.Key)
            .Select(f => new FeatureFlagResponse(f.Key, f.Enabled, f.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<FeatureFlagResponse>>.Success(flags);
    }
}

public sealed record FeatureFlagResponse(string Key, bool Enabled, DateTime UpdatedAtUtc);
