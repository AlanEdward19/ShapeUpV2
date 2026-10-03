namespace ShapeUp.Features.Credentials.Shared.Errors;

public enum CredentialConflictKind
{
    /// <summary>The user already has an open or verified credential for the profession.</summary>
    OpenForUserAndProfession,

    /// <summary>Another user holds an open or verified credential with the same council registration.</summary>
    RegistrationInUse,

    /// <summary>The row was changed by someone else since it was read.</summary>
    ConcurrentChange
}

/// <summary>Raised by the repository when the database rejects a write that a concurrent request won; handlers turn it into a 409.</summary>
public sealed class CredentialConflictException(CredentialConflictKind kind, Exception? inner = null)
    : Exception($"Credential write conflict: {kind}.", inner)
{
    public CredentialConflictKind Kind { get; } = kind;

    public ShapeUp.Shared.Results.Error ToError(string professionType = "") => Kind switch
    {
        CredentialConflictKind.OpenForUserAndProfession => CredentialErrors.AlreadyInProgressOrVerified(professionType),
        CredentialConflictKind.RegistrationInUse => CredentialErrors.RegistrationInUse(),
        _ => CredentialErrors.ConcurrentChange()
    };
}
