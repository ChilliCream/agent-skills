using System.Security.Claims;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Mocha.Mediator;
using Reference.GraphQLFirst.Application.Books.Queries;
using Reference.GraphQLFirst.Domain.Books;

namespace Reference.GraphQLFirst.GraphQL.Books.Operations;

[QueryType]
public static partial class BookQueries
{
    public static async ValueTask<Book?> GetBookByIdAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID<Book>] Guid id,
        CancellationToken cancellationToken)
        => await sender.QueryAsync(new GetBookByIdQuery(user, id), cancellationToken);
}
