using System.Security.Claims;
using Mocha.Mediator;
using Reference.Hexagonal.Adapters.GraphQL.Authors.Commands;
using Reference.Hexagonal.Core.Domain;
using Reference.Hexagonal.Core.Exceptions;

namespace Reference.Hexagonal.Adapters.GraphQL.Authors.Operations;

[MutationType]
public sealed class AuthorMutations
{
    [Authorize]
    [Error<DuplicateAuthorNameException>]
    [Error<UnauthorizedAccessException>]
    public ValueTask<Author> CreateAuthorAsync(
        ClaimsPrincipal user,
        ISender sender,
        string name,
        string? biography,
        CancellationToken cancellationToken)
        => sender.SendAsync(
            new CreateAuthorCommand(user, name, biography),
            cancellationToken);
}
