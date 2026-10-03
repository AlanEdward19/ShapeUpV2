namespace ShapeUp.Features.Credentials.GetMyCredentials;

using Shared.Abstractions;
using Shared.Entities;
using ShapeUp.Shared.Results;

public class GetMyCredentialsHandler(IProfessionalCredentialRepository repository)
{
    public async Task<Result<IReadOnlyList<CredentialResponse>>> HandleAsync(int userId, CancellationToken cancellationToken)
    {
        var credentials = await repository.GetByUserIdAsync(userId, cancellationToken);
        return Result<IReadOnlyList<CredentialResponse>>.Success(credentials.Select(c => CredentialResponse.From(c)).ToList());
    }
}
