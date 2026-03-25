namespace ShapeUp.Features.Authorization.Scopes.CreateScope;

using Shared.Abstractions;
using Shared.Entities;
using Shared.Errors;
using ShapeUp.Shared.Results;

public class CreateScopeHandler(IScopeRepository scopeRepository)
{
    public async Task<Result<CreateScopeResponse>> HandleAsync(
        CreateScopeCommand command,
        CancellationToken cancellationToken)
    {
        var existingScope = await scopeRepository.GetByScopeFormatAsync(command.Domain, command.Subdomain, command.Action, cancellationToken);
        if (existingScope != null)
        {
            var scopeName = $"{command.Domain}:{command.Subdomain}:{command.Action}";
            return Result<CreateScopeResponse>.Failure(AuthorizationErrors.ScopeAlreadyExists(scopeName));
        }

        var name = $"{command.Domain}:{command.Subdomain}:{command.Action}";
        var scope = new Scope
        {
            Name = name,
            Domain = command.Domain,
            Subdomain = command.Subdomain,
            Action = command.Action,
            Description = command.Description
        };

        await scopeRepository.AddAsync(scope, cancellationToken);

        var response = new CreateScopeResponse(scope.Id, scope.Name, scope.Domain, scope.Subdomain, scope.Action, scope.Description);
        return Result<CreateScopeResponse>.Success(response);
    }
}
