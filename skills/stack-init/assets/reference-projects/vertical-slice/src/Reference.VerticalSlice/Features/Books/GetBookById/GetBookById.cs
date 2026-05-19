using Reference.VerticalSlice.Domain;
using Reference.VerticalSlice.Features.Books.DataLoaders;
using Reference.VerticalSlice.Shared.Security;

namespace Reference.VerticalSlice.Features.Books.GetBookById;

public sealed record GetBookByIdQuery(ClaimsPrincipal User, Guid BookId) : IQuery<Book?>;

public sealed class GetBookByIdQueryHandler(
    IBookBatchingContext books,
    IAuthorizationService authorization)
    : IQueryHandler<GetBookByIdQuery, Book?>
{
    public async ValueTask<Book?> HandleAsync(
        GetBookByIdQuery query,
        CancellationToken cancellationToken)
    {
        var (user, bookId) = query;

        if (user.Identity is not { IsAuthenticated: true })
        {
            return null;
        }

        var book = await books.BookById.LoadAsync(bookId, cancellationToken);
        if (book is null)
        {
            return null;
        }

        var authorized = await authorization.AuthorizeAsync(user, book, LibraryPolicies.Read);
        return authorized.Succeeded ? book : null;
    }
}

[QueryType]
public static class GetBookByIdQueryType
{
    public static async ValueTask<Book?> GetBookByIdAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID(nameof(Book))] Guid bookId,
        CancellationToken cancellationToken)
    {
        return await sender.QueryAsync(
            new GetBookByIdQuery(user, bookId),
            cancellationToken);
    }
}
