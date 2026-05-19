using Reference.VerticalSlice.Domain;
using Reference.VerticalSlice.Features.Authors.GetAuthorById;
using Reference.VerticalSlice.Features.Books.ListBooksByAuthor;

namespace Reference.VerticalSlice.Features.Authors.Types;

[ObjectType<Author>]
public static partial class AuthorType
{
    static partial void Configure(IObjectTypeDescriptor<Author> descriptor)
    {
        descriptor.Ignore(author => author.Books);
    }

    [NodeResolver]
    public static async ValueTask<Author?> GetAuthorByIdAsync(
        Guid id,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        return await sender.QueryAsync(new GetAuthorByIdQuery(user, id), cancellationToken);
    }

    public static async ValueTask<IReadOnlyList<Book>?> GetBooksAsync(
        [Parent] Author author,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        return await sender.QueryAsync(
            new ListBooksByAuthorQuery(user, author.Id),
            cancellationToken);
    }
}
