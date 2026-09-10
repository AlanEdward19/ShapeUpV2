namespace ShapeUp.Features.PlatformFeatureFlags.SetFeatureFlag;

using FluentValidation;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Entities;
using ShapeUp.Shared.Results;

public class SetFeatureFlagHandler(
    PlatformFeatureFlagsDbContext context,
    IValidator<SetFeatureFlagCommand> validator)
{
    public async Task<Result<SetFeatureFlagResponse>> HandleAsync(SetFeatureFlagCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<SetFeatureFlagResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        var existing = await context.Flags.FirstOrDefaultAsync(f => f.Key == command.Key, cancellationToken);
        var updatedAtUtc = DateTime.UtcNow;

        if (existing is null)
        {
            context.Flags.Add(new PlatformFeatureFlag
            {
                Key = command.Key,
                Enabled = command.Enabled,
                UpdatedAtUtc = updatedAtUtc,
                UpdatedByUserId = command.UpdatedByUserId
            });
        }
        else
        {
            existing.Enabled = command.Enabled;
            existing.UpdatedAtUtc = updatedAtUtc;
            existing.UpdatedByUserId = command.UpdatedByUserId;
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result<SetFeatureFlagResponse>.Success(
            new SetFeatureFlagResponse(command.Key, command.Enabled, updatedAtUtc));
    }
}

public sealed record SetFeatureFlagResponse(string Key, bool Enabled, DateTime UpdatedAtUtc);
