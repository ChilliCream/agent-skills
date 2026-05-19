using System.Security.Claims;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Mocha.Mediator;
using Reference.GraphQLFirst.Application.Authors.Queries;
using Reference.GraphQLFirst.Domain.Authors;

namespace Reference.GraphQLFirst.GraphQL.Authors.Operations;

[QueryType]
public static partial class AuthorQueries
{
    public static async ValueTask<Author?> GetAuthorByIdAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID<Author>] Guid id,
        CancellationToken cancellationToken)
        => await sender.QueryAsync(new GetAuthorByIdQuery(user, id), cancellationToken);
}
