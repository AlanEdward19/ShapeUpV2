namespace ShapeUp.Features.Credentials.Shared.Abstractions;

using Entities;

public interface ICredentialVerificationAuditRepository
{
    Task AddAsync(CredentialVerificationAudit audit, CancellationToken cancellationToken);

    Task<IReadOnlyList<CredentialVerificationAudit>> GetByCredentialAsync(int credentialId, CancellationToken cancellationToken);
}
