using Mocha.Mediator;
using Reference.Hexagonal.Adapters.GraphQL.Books.Queries;
using Reference.Hexagonal.Core.Domain;

namespace Reference.Hexagonal.Adapters.GraphQL.Books.Operations;

[QueryType]
public sealed class BookQueries
{
    public ValueTask<Book?> GetBookByIdAsync(
        ISender sender,
        [ID<Book>] Guid id,
        CancellationToken cancellationToken)
        => sender.QueryAsync(new GetBookByIdQuery(id), cancellationToken);

    [UsePaging]
    public ValueTask<IReadOnlyList<Book>> GetBooksByAuthorIdAsync(
        ISender sender,
        [ID<Author>] Guid authorId,
        CancellationToken cancellationToken)
        => sender.QueryAsync(new GetBooksByAuthorIdQuery(authorId), cancellationToken);
}
