using System.Security.Claims;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Mocha.Mediator;
using Reference.CleanArchitecture.Application.Books.Queries;
using Reference.CleanArchitecture.Domain.Books;

namespace Reference.CleanArchitecture.GraphQL.Books.Operations;

[QueryType]
public sealed class BookQueries
{
    public async ValueTask<Book?> GetBookByIdAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID<Book>] Guid id,
        CancellationToken cancellationToken)
    {
        return await sender.QueryAsync(
            new GetBookByIdQuery(user, id),
            cancellationToken);
    }
}
