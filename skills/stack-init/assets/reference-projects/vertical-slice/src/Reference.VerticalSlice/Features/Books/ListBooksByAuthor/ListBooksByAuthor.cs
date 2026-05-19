using Reference.VerticalSlice.Domain;
using Reference.VerticalSlice.Features.Authors.DataLoaders;
using Reference.VerticalSlice.Features.Books.DataLoaders;
using Reference.VerticalSlice.Shared.Security;

namespace Reference.VerticalSlice.Features.Books.ListBooksByAuthor;

public sealed record ListBooksByAuthorQuery(
    ClaimsPrincipal User,
    Guid AuthorId) : IQuery<IReadOnlyList<Book>?>;

public sealed class ListBooksByAuthorQueryHandler(
    IAuthorBatchingContext authors,
    IBookBatchingContext books,
    IAuthorizationService authorization)
    : IQueryHandler<ListBooksByAuthorQuery, IReadOnlyList<Book>?>
{
    public async ValueTask<IReadOnlyList<Book>?> HandleAsync(
        ListBooksByAuthorQuery query,
        CancellationToken cancellationToken)
    {
        var (user, authorId) = query;

        if (user.Identity is not { IsAuthenticated: true })
        {
            return null;
        }

        var author = await authors.AuthorById.LoadAsync(authorId, cancellationToken);
        if (author is null)
        {
            return null;
        }

        var authorized = await authorization.AuthorizeAsync(user, author, LibraryPolicies.Read);
        if (!authorized.Succeeded)
        {
            return null;
        }

        return await books.BooksByAuthorId.LoadAsync(authorId, cancellationToken);
    }
}

[QueryType]
public static class ListBooksByAuthorQueryType
{
    public static async ValueTask<IReadOnlyList<Book>?> GetBooksByAuthorIdAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID(nameof(Author))] Guid authorId,
        CancellationToken cancellationToken)
    {
        return await sender.QueryAsync(
            new ListBooksByAuthorQuery(user, authorId),
            cancellationToken);
    }
}
