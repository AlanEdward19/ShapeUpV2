namespace ShapeUp.Features.Credentials.Shared.Lifecycle;

using Entities;

/// <summary>Keeps the Trainer/Nutritionist platform role in step with a credential.</summary>
public interface IProfessionalRoleGranter
{
    /// <summary>Grants the role for a verified credential. Idempotent; never touches a role that already exists.</summary>
    Task GrantAsync(ProfessionalCredential credential, CancellationToken cancellationToken);

    /// <summary>
    /// Withdraws the role only if this credential granted it. A role assigned by an admin is kept, and if
    /// the user has another valid credential for the profession the role passes to it instead.
    /// </summary>
    Task ReleaseAsync(ProfessionalCredential credential, DateTime nowUtc, CancellationToken cancellationToken);
}
