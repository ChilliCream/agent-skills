using Reference.VerticalSlice.Domain;
using Reference.VerticalSlice.Features.Authors.GetAuthorById;
using Reference.VerticalSlice.Features.Books.GetBookById;

namespace Reference.VerticalSlice.Features.Books.Types;

[ObjectType<Book>]
public static partial class BookType
{
    static partial void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor.Ignore(book => book.AuthorId);
        descriptor.Ignore(book => book.Author);
    }

    [NodeResolver]
    public static async ValueTask<Book?> GetBookByIdAsync(
        Guid id,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        return await sender.QueryAsync(new GetBookByIdQuery(user, id), cancellationToken);
    }

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
