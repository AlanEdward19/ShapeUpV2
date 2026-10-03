namespace ShapeUp.Features.Credentials.Shared.Errors;

using Microsoft.AspNetCore.Http;
using ShapeUp.Shared.Results;

public static class CredentialErrors
{
    public static Error NotFound(int id) =>
        CommonErrors.NotFound($"Credential with ID {id} was not found.");

    public static Error AlreadyInProgressOrVerified(string professionType) =>
        CommonErrors.Conflict($"You already have a '{professionType}' credential awaiting review or verified.");

    public static Error RegistrationInUse() =>
        CommonErrors.Conflict("This council registration is already in use by another account that is awaiting review or verified.");

    public static Error InvalidTransition(string from, string to) =>
        CommonErrors.Conflict($"A credential in status '{from}' cannot move to '{to}'.");

    public static Error ConcurrentChange() =>
        CommonErrors.Conflict("The credential was changed by another request. Reload it and try again.");

    /// <summary>The status change was undone because the platform role could not be kept in step with it.</summary>
    public static Error RoleSyncFailed() =>
        new("credential_role_sync_failed",
            "The credential change was not applied because the professional role could not be updated. Try again.",
            StatusCodes.Status503ServiceUnavailable);
}
