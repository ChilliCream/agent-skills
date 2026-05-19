using Reference.VerticalSlice.Domain;
using Reference.VerticalSlice.Features.Authors;
using Reference.VerticalSlice.Shared.Persistence;
using Reference.VerticalSlice.Shared.Security;

namespace Reference.VerticalSlice.Features.Books.CreateBook;

public sealed record CreateBookCommand(
    ClaimsPrincipal User,
    Guid AuthorId,
    string Title) : ICommand<Book>;

public sealed class CreateBookCommandHandler(
    AppDbContext context,
    IPromiseCache cache,
    IAuthorizationService authorization)
    : ICommandHandler<CreateBookCommand, Book>
{
    public async ValueTask<Book> HandleAsync(
        CreateBookCommand command,
        CancellationToken cancellationToken)
    {
        var (user, authorId, title) = command;

        if (user.Identity is not { IsAuthenticated: true })
        {
            throw new UnauthorizedAccessException();
        }

        var author = await context.Authors
            .FirstOrDefaultAsync(candidate => candidate.Id == authorId, cancellationToken);

        if (author is null)
        {
            throw new AuthorNotFoundException(authorId);
        }

        cache.Publish(author);

        var authorized = await authorization.AuthorizeAsync(user, author, LibraryPolicies.Write);
        if (!authorized.Succeeded)
        {
            throw new AuthorNotFoundException(authorId);
        }

        var book = Book.Create(author.Id, title);

        context.Books.Add(book);
        cache.Publish(book);
        await context.SaveChangesAsync(cancellationToken);

        return book;
    }
}

[MutationType]
public static class CreateBookMutation
{
    [HotChocolate.Authorization.Authorize]
    [Error<AuthorNotFoundException>]
    [Error<UnauthorizedAccessException>]
    [Error<ArgumentException>]
    public static async ValueTask<Book> CreateBookAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID(nameof(Author))] Guid authorId,
        string title,
        CancellationToken cancellationToken)
    {
        return await sender.SendAsync(
            new CreateBookCommand(user, authorId, title),
            cancellationToken);
    }
}
