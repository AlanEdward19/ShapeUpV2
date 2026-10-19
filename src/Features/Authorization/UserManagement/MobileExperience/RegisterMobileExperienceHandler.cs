using FluentValidation;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Authorization.UserManagement.MobileExperience;

public class RegisterMobileExperienceHandler(
    IUserRepository users, IValidator<RegisterMobileExperienceCommand> validator)
{
    public async Task<Result<MobileExperienceResponse>> HandleAsync(
        RegisterMobileExperienceCommand command, int actorUserId, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<MobileExperienceResponse>.Failure(CommonErrors.Validation(
                string.Join("; ", validation.Errors.Select(error => error.ErrorMessage))));
        var user = await users.GetByIdAsync(actorUserId, cancellationToken);
        if (user is null || !user.IsActive)
            return Result<MobileExperienceResponse>.Failure(CommonErrors.Forbidden("Conta indisponível."));
        var stamp = DateTime.UtcNow;
        var first = await users.TryRegisterMobileExperienceAsync(actorUserId, stamp, cancellationToken);
        user = await users.GetByIdAsync(actorUserId, cancellationToken);
        if (user?.FirstMobileSeenAtUtc is not { } firstSeen)
            return Result<MobileExperienceResponse>.Failure(CommonErrors.Forbidden("Conta indisponível."));
        return Result<MobileExperienceResponse>.Success(new(first, DateTime.SpecifyKind(firstSeen, DateTimeKind.Utc)));
    }
}
