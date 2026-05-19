using System.Security.Claims;
using Mocha.Mediator;
using Reference.Hexagonal.Adapters.GraphQL.Books.Commands;
using Reference.Hexagonal.Core.Domain;
using Reference.Hexagonal.Core.Exceptions;

namespace Reference.Hexagonal.Adapters.GraphQL.Books.Operations;

[MutationType]
public sealed class BookMutations
{
    [Authorize]
    [Error<AuthorNotFoundException>]
    [Error<DuplicateIsbnException>]
    [Error<UnauthorizedAccessException>]
    public ValueTask<Book> CreateBookAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID<Author>] Guid authorId,
        string isbn,
        string title,
        string? synopsis,
        DateOnly? publishedOn,
        CancellationToken cancellationToken)
        => sender.SendAsync(
            new CreateBookCommand(user, authorId, isbn, title, synopsis, publishedOn),
            cancellationToken);

    [Authorize]
    [Error<BookNotFoundException>]
    [Error<UnauthorizedAccessException>]
    public ValueTask<Book> RenameBookAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID<Book>] Guid bookId,
        string title,
        CancellationToken cancellationToken)
        => sender.SendAsync(
            new RenameBookCommand(user, bookId, title),
            cancellationToken);
}
