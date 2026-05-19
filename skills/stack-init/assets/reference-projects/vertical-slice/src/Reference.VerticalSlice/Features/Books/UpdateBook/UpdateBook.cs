using Reference.VerticalSlice.Domain;
using Reference.VerticalSlice.Features.Books;
using Reference.VerticalSlice.Shared.Persistence;
using Reference.VerticalSlice.Shared.Security;

namespace Reference.VerticalSlice.Features.Books.UpdateBook;

public sealed record UpdateBookTitleCommand(
    ClaimsPrincipal User,
    Guid BookId,
    string Title) : ICommand<Book>;

public sealed class UpdateBookTitleCommandHandler(
    AppDbContext context,
    IPromiseCache cache,
    IAuthorizationService authorization)
    : ICommandHandler<UpdateBookTitleCommand, Book>
{
    public async ValueTask<Book> HandleAsync(
        UpdateBookTitleCommand command,
        CancellationToken cancellationToken)
    {
        var (user, bookId, title) = command;

        if (user.Identity is not { IsAuthenticated: true })
        {
            throw new UnauthorizedAccessException();
        }

        var book = await context.Books
            .FirstOrDefaultAsync(candidate => candidate.Id == bookId, cancellationToken);

        if (book is null)
        {
            throw new BookNotFoundException(bookId);
        }

        cache.Publish(book);

        var authorized = await authorization.AuthorizeAsync(user, book, LibraryPolicies.Write);
        if (!authorized.Succeeded)
        {
            throw new BookNotFoundException(bookId);
        }

        book.Rename(title);
        cache.Publish(book);

        await context.SaveChangesAsync(cancellationToken);
        return book;
    }
}

[MutationType]
public static class UpdateBookMutation
{
    [HotChocolate.Authorization.Authorize]
    [Error<BookNotFoundException>]
    [Error<UnauthorizedAccessException>]
    [Error<ArgumentException>]
    public static async ValueTask<Book> UpdateBookTitleAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID(nameof(Book))] Guid bookId,
        string title,
        CancellationToken cancellationToken)
    {
        return await sender.SendAsync(
            new UpdateBookTitleCommand(user, bookId, title),
            cancellationToken);
    }
}
