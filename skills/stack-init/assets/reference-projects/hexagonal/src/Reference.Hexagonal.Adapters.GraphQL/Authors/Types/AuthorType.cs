using Mocha.Mediator;
using Reference.Hexagonal.Adapters.GraphQL.Authors.Queries;
using Reference.Hexagonal.Adapters.GraphQL.Books.Queries;
using Reference.Hexagonal.Core.Domain;

namespace Reference.Hexagonal.Adapters.GraphQL.Authors.Types;

[ObjectType<Author>]
public static partial class AuthorType
{
    [NodeResolver]
    public static ValueTask<Author?> GetAuthorByIdAsync(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
        => sender.QueryAsync(new GetAuthorByIdQuery(id), cancellationToken);

    [UsePaging]
    public static ValueTask<IReadOnlyList<Book>> GetBooksAsync(
        [Parent] Author author,
        ISender sender,
        CancellationToken cancellationToken)
        => sender.QueryAsync(new GetBooksByAuthorIdQuery(author.Id), cancellationToken);
}
