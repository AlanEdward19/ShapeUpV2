namespace ShapeUp.Features.Credentials.Shared.Lifecycle;

using Abstractions;
using Entities;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;

public class ProfessionalRoleGranter(
    IUserPlatformRoleRepository roleRepository,
    IProfessionalCredentialRepository credentialRepository) : IProfessionalRoleGranter
{
    public static PlatformRoleType? RoleFor(string professionType) => professionType switch
    {
        CredentialAuthorities.PersonalTrainer => PlatformRoleType.Trainer,
        CredentialAuthorities.Nutritionist => PlatformRoleType.Nutritionist,
        _ => null
    };

    public async Task GrantAsync(ProfessionalCredential credential, CancellationToken cancellationToken)
    {
        if (RoleFor(credential.ProfessionType) is not { } role)
            return;

        var existing = await roleRepository.GetByUserIdAndRoleAsync(credential.UserId, role, cancellationToken);
        if (existing is null)
        {
            await roleRepository.AddAsync(new UserPlatformRole
            {
                UserId = credential.UserId,
                Role = role,
                GrantedByCredentialId = credential.Id
            }, cancellationToken);
            return;
        }

        // A role previously taken back from a credential comes back with the new one.
        if (existing.GrantedByCredentialId is not null && !existing.IsActive)
        {
            existing.IsActive = true;
            existing.GrantedByCredentialId = credential.Id;
            await roleRepository.UpdateAsync(existing, cancellationToken);
        }
    }

    public async Task ReleaseAsync(ProfessionalCredential credential, DateTime nowUtc, CancellationToken cancellationToken)
    {
        if (RoleFor(credential.ProfessionType) is not { } role)
            return;

        var existing = await roleRepository.GetByUserIdAndRoleAsync(credential.UserId, role, cancellationToken);
        if (existing is null || existing.GrantedByCredentialId != credential.Id)
            return;

        var other = await credentialRepository.GetVerifiedAsync(credential.UserId, credential.ProfessionType, nowUtc, cancellationToken);
        if (other is not null && other.Id != credential.Id)
        {
            existing.GrantedByCredentialId = other.Id;
            await roleRepository.UpdateAsync(existing, cancellationToken);
            return;
        }

        await roleRepository.DeleteAsync(existing.Id, cancellationToken);
    }
}
