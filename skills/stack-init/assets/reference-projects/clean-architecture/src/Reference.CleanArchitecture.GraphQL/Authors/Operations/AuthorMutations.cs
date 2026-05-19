using System.Security.Claims;
using HotChocolate.Authorization;
using HotChocolate.Types;
using Mocha.Mediator;
using Reference.CleanArchitecture.Application.Authors.Commands;
using Reference.CleanArchitecture.Domain.Authors;

namespace Reference.CleanArchitecture.GraphQL.Authors.Operations;

[MutationType]
public sealed class AuthorMutations
{
    [Authorize]
    [Error<UnauthorizedAccessException>]
    public async ValueTask<Author> RegisterAuthorAsync(
        ClaimsPrincipal user,
        ISender sender,
        string name,
        CancellationToken cancellationToken)
    {
        return await sender.SendAsync(
            new RegisterAuthorCommand(user, name),
            cancellationToken);
    }
}
