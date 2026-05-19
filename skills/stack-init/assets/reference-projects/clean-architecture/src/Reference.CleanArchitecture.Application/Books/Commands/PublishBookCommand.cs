using System.Security.Claims;
using GreenDonut;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Reference.CleanArchitecture.Application.Books.Errors;
using Reference.CleanArchitecture.Application.Common;
using Reference.CleanArchitecture.Domain.Books;
using Mocha.Mediator;

namespace Reference.CleanArchitecture.Application.Books.Commands;

public sealed record PublishBookCommand(ClaimsPrincipal User, Guid BookId) : ICommand<Book>;

public sealed class PublishBookCommandHandler(
    IAppDbContext context,
    IPromiseCache cache,
    IAuthorizationService authorization)
    : ICommandHandler<PublishBookCommand, Book>
{
    public async ValueTask<Book> HandleAsync(
        PublishBookCommand command,
        CancellationToken cancellationToken)
    {
        var (user, bookId) = command;

        if (user.Identity is not { IsAuthenticated: true })
        {
            throw new UnauthorizedAccessException();
        }

        var book = await context.Books
            .FirstOrDefaultAsync(x => x.Id == bookId, cancellationToken);

        if (book is null)
        {
            throw new BookNotFoundException(bookId);
        }

        cache.Publish(book);

        var authorized = await authorization.AuthorizeAsync(
            user,
            book,
            BookStorePolicies.BooksWrite);

        if (!authorized.Succeeded)
        {
            throw new BookNotFoundException(bookId);
        }

        book.Publish(DateTimeOffset.UtcNow);

        await context.SaveChangesAsync(cancellationToken);

        return book;
    }
}
