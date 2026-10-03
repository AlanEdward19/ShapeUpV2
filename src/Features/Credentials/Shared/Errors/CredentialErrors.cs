namespace ShapeUp.Features.Credentials.Shared.Errors;

using ShapeUp.Shared.Results;

public static class CredentialErrors
{
    public static Error NotFound(int id) =>
        CommonErrors.NotFound($"Credential with ID {id} was not found.");

    public static Error AlreadyInProgressOrVerified(string professionType) =>
        CommonErrors.Conflict($"You already have a '{professionType}' credential awaiting review or verified.");

    public static Error InvalidTransition(string from, string to) =>
        CommonErrors.Conflict($"A credential in status '{from}' cannot move to '{to}'.");
}
