namespace ShapeUp.Features.GymManagement.GymStaff.AddGymStaff;

using FluentValidation;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.GymManagement.Shared.Errors;
using ShapeUp.Shared.Results;

public class AddGymStaffHandler(
    IGymStaffRepository staffRepository,
    IGymRepository gymRepository,
    IValidator<AddGymStaffCommand> validator)
{
    public async Task<Result<AddGymStaffResponse>> HandleAsync(AddGymStaffCommand command, int currentUserId, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<AddGymStaffResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        var gym = await gymRepository.GetByIdAsync(command.GymId, cancellationToken);
        if (gym is null) return Result<AddGymStaffResponse>.Failure(GymManagementErrors.GymNotFound(command.GymId));

        var canManage = await staffRepository.IsOwnerOrReceptionistAsync(command.GymId, currentUserId, gym.OwnerId, cancellationToken);
        if (!canManage) return Result<AddGymStaffResponse>.Failure(GymManagementErrors.NotGymOwnerOrReceptionist(currentUserId, command.GymId));

        var alreadyStaff = await staffRepository.IsStaffAsync(command.GymId, command.UserId, cancellationToken);
        if (alreadyStaff) return Result<AddGymStaffResponse>.Failure(GymManagementErrors.UserAlreadyStaffInGym(command.UserId, command.GymId));

        var staff = new GymStaff { GymId = command.GymId, UserId = command.UserId, Role = command.Role };
        await staffRepository.AddAsync(staff, cancellationToken);
        return Result<AddGymStaffResponse>.Success(new AddGymStaffResponse(staff.Id, staff.GymId, staff.UserId, staff.Role.ToString()));
    }
}


