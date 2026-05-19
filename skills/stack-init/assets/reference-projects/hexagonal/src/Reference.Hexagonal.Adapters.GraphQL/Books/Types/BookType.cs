using Mocha.Mediator;
using Reference.Hexagonal.Adapters.GraphQL.Authors.Queries;
using Reference.Hexagonal.Adapters.GraphQL.Books.Queries;
using Reference.Hexagonal.Core.Domain;

namespace Reference.Hexagonal.Adapters.GraphQL.Books.Types;

[ObjectType<Book>]
public static partial class BookType
{
    static partial void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor.Ignore(x => x.AuthorId);
    }

    [NodeResolver]
    public static ValueTask<Book?> GetBookByIdAsync(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
        => sender.QueryAsync(new GetBookByIdQuery(id), cancellationToken);

    public static ValueTask<Author?> GetAuthorAsync(
        [Parent] Book book,
        ISender sender,
        CancellationToken cancellationToken)
        => sender.QueryAsync(new GetAuthorByIdQuery(book.AuthorId), cancellationToken);
}
