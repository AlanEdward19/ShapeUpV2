namespace ShapeUp.Features.Credentials.Shared.Lifecycle;

using Entities;
using ShapeUp.Features.Authorization.Shared.Abstractions;

/// <summary>Builds admin-facing responses that carry the requester's name and e-mail (one query for the whole page).</summary>
public class CredentialRequesterEnricher(IUserRepository userRepository)
{
    public async Task<CredentialResponse> ToResponseAsync(ProfessionalCredential credential, CancellationToken cancellationToken) =>
        (await ToResponsesAsync([credential], cancellationToken))[0];

    public async Task<CredentialResponse[]> ToResponsesAsync(
        IReadOnlyCollection<ProfessionalCredential> credentials, CancellationToken cancellationToken)
    {
        var ids = credentials.Select(c => c.UserId).Distinct().ToArray();
        var users = (await userRepository.GetByIdsAsync(ids, cancellationToken)).ToDictionary(u => u.Id);

        return credentials
            .Select(c => users.TryGetValue(c.UserId, out var user)
                ? CredentialResponse.From(c, user.DisplayName, user.Email)
                : CredentialResponse.From(c))
            .ToArray();
    }
}
