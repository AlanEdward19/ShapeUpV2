namespace ShapeUp.Features.Credentials.GetCredentialsUnderReview;

using Shared.Abstractions;
using Shared.Entities;
using ShapeUp.Shared.Results;

public class GetCredentialsUnderReviewHandler(IProfessionalCredentialRepository repository)
{
    public async Task<Result<IReadOnlyList<CredentialResponse>>> HandleAsync(CancellationToken cancellationToken)
    {
        var credentials = await repository.GetByStatusAsync(CredentialStatus.UnderReview, cancellationToken);
        return Result<IReadOnlyList<CredentialResponse>>.Success(credentials.Select(CredentialResponse.From).ToList());
    }
}
