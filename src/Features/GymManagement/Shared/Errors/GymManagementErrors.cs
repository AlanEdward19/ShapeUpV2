namespace ShapeUp.Features.GymManagement.Shared.Errors;

using ShapeUp.Shared.Results;

public static class GymManagementErrors
{
    public static Error PlatformTierNotFound(int id) =>
        CommonErrors.NotFound($"Platform tier with ID {id} was not found.");

    public static Error PlatformTierNameAlreadyExists(string name) =>
        CommonErrors.Conflict($"A platform tier named '{name}' already exists.");

    public static Error PlatformTierRoleMismatch(int tierId, string tierTargetRole, string assignedRole) =>
        CommonErrors.Validation($"Platform tier {tierId} is intended for '{tierTargetRole}' users and cannot be assigned to a '{assignedRole}' role.");

    public static Error GymNotFound(int id) =>
        CommonErrors.NotFound($"Gym with ID {id} was not found.");

    public static Error GymPlanNotFound(int id) =>
        CommonErrors.NotFound($"Gym plan with ID {id} was not found.");

    public static Error GymPlanDoesNotBelongToGym(int planId, int gymId) =>
        CommonErrors.Validation($"Plan {planId} does not belong to gym {gymId}.");

    public static Error GymStaffNotFound(int gymId, int userId) =>
        CommonErrors.NotFound($"Staff member (user {userId}) not found in gym {gymId}.");

    public static Error GymClientNotFound(int gymId, int userId) =>
        CommonErrors.NotFound($"Client (user {userId}) not found in gym {gymId}.");

    public static Error UserAlreadyHasRole(int userId, string role) =>
        CommonErrors.Conflict($"User {userId} already has the '{role}' role.");

    public static Error UserAlreadyStaffInGym(int userId, int gymId) =>
        CommonErrors.Conflict($"User {userId} is already a staff member in gym {gymId}.");

    public static Error UserAlreadyClientInGym(int userId, int gymId) =>
        CommonErrors.Conflict($"User {userId} is already enrolled in gym {gymId}.");

    public static Error TrainerPlanNotFound(int id) =>
        CommonErrors.NotFound($"Trainer plan with ID {id} was not found.");

    public static Error TrainerPlanDoesNotBelongToTrainer(int planId, int trainerId) =>
        CommonErrors.Validation($"Plan {planId} does not belong to trainer {trainerId}.");

    public static Error TrainerClientNotFound(int trainerId, int clientId) =>
        CommonErrors.NotFound($"Client {clientId} not found under trainer {trainerId}.");

    public static Error ClientAlreadyUnderTrainer(int clientId, int trainerId) =>
        CommonErrors.Conflict($"Client {clientId} is already registered under trainer {trainerId}.");

    public static Error NotGymOwnerOrReceptionist(int userId, int gymId) =>
        CommonErrors.Forbidden($"User {userId} is not an owner or receptionist of gym {gymId}.");

    public static Error StaffMemberIsNotTrainer(int staffId) =>
        CommonErrors.Validation($"Staff member {staffId} is not a trainer.");

    public static Error UserNotGymOwner(int userId, int gymId) =>
        CommonErrors.Forbidden($"User {userId} is not the owner of gym {gymId}.");

    public static Error NotGymOwnerOrStaff(int userId, int gymId) =>
        CommonErrors.Forbidden($"User {userId} is not an owner or active staff member of gym {gymId}.");

    public static Error TrainerNotFound(int trainerId) =>
        CommonErrors.NotFound($"Trainer with user ID {trainerId} was not found.");
}

