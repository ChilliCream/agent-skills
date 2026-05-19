using System.Security.Claims;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Mocha.Mediator;
using Reference.CleanArchitecture.Application.Authors.Queries;
using Reference.CleanArchitecture.Domain.Authors;

namespace Reference.CleanArchitecture.GraphQL.Authors.Operations;

[QueryType]
public sealed class AuthorQueries
{
    public async ValueTask<Author?> GetAuthorByIdAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID<Author>] Guid id,
        CancellationToken cancellationToken)
    {
        return await sender.QueryAsync(
            new GetAuthorByIdQuery(user, id),
            cancellationToken);
    }
}
