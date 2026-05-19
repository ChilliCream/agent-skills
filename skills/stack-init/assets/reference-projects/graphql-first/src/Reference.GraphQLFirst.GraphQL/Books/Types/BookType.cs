using System.Security.Claims;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Mocha.Mediator;
using Reference.GraphQLFirst.Application.Authors.Queries;
using Reference.GraphQLFirst.Application.Books.Queries;
using Reference.GraphQLFirst.Domain.Authors;
using Reference.GraphQLFirst.Domain.Books;

namespace Reference.GraphQLFirst.GraphQL.Books.Types;

[ObjectType<Book>]
public static partial class BookType
{
    [NodeResolver]
    public static async ValueTask<Book?> GetBookByIdAsync(
        Guid id,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
        => await sender.QueryAsync(new GetBookByIdQuery(user, id), cancellationToken);

    [BindMember(nameof(Book.AuthorId))]
    public static async ValueTask<Author?> GetAuthorAsync(
        [Parent] Book book,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
        => await sender.QueryAsync(
            new GetAuthorByIdQuery(user, book.AuthorId),
            cancellationToken);
}
