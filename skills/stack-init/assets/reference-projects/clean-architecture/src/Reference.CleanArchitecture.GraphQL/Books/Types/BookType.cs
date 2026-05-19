using System.Security.Claims;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Mocha.Mediator;
using Reference.CleanArchitecture.Application.Authors.Queries;
using Reference.CleanArchitecture.Application.Books.Queries;
using Reference.CleanArchitecture.Domain.Authors;
using Reference.CleanArchitecture.Domain.Books;

namespace Reference.CleanArchitecture.GraphQL.Books.Types;

[ObjectType<Book>]
public static partial class BookType
{
    static partial void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor.Ignore(x => x.AuthorId);
        descriptor.Ignore(x => x.Isbn);
        descriptor.Ignore(x => x.Events);
    }

    [NodeResolver]
    public static async ValueTask<Book?> GetBookByIdAsync(
        Guid id,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        return await sender.QueryAsync(
            new GetBookByIdQuery(user, id),
            cancellationToken);
    }

    public static string GetIsbn([Parent] Book book) => book.Isbn.Value;

    public static async ValueTask<Author?> GetAuthorAsync(
        [Parent] Book book,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        return await sender.QueryAsync(
            new GetAuthorByIdQuery(user, book.AuthorId),
            cancellationToken);
    }
}
