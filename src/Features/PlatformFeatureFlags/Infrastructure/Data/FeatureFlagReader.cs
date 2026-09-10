namespace ShapeUp.Features.PlatformFeatureFlags.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Shared.Abstractions;

public class FeatureFlagReader(PlatformFeatureFlagsDbContext context) : IFeatureFlagReader
{
    public async Task<bool> IsEnabledAsync(string key, CancellationToken cancellationToken)
    {
        var flag = await context.Flags
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Key == key, cancellationToken);

        return flag?.Enabled ?? true;
    }
}
