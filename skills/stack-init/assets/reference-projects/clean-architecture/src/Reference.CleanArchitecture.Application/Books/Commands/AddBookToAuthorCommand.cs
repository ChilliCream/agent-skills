using System.Security.Claims;
using GreenDonut;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Reference.CleanArchitecture.Application.Authors.Errors;
using Reference.CleanArchitecture.Application.Common;
using Reference.CleanArchitecture.Domain.Books;
using Mocha.Mediator;

namespace Reference.CleanArchitecture.Application.Books.Commands;

public sealed record AddBookToAuthorCommand(
    ClaimsPrincipal User,
    Guid AuthorId,
    string Title,
    string Isbn) : ICommand<Book>;

public sealed class AddBookToAuthorCommandHandler(
    IAppDbContext context,
    IPromiseCache cache,
    IAuthorizationService authorization)
    : ICommandHandler<AddBookToAuthorCommand, Book>
{
    public async ValueTask<Book> HandleAsync(
        AddBookToAuthorCommand command,
        CancellationToken cancellationToken)
    {
        var (user, authorId, title, isbn) = command;

        if (user.Identity is not { IsAuthenticated: true })
        {
            throw new UnauthorizedAccessException();
        }

        var author = await context.Authors
            .Include(x => x.Books)
            .FirstOrDefaultAsync(x => x.Id == authorId, cancellationToken);

        if (author is null)
        {
            throw new AuthorNotFoundException(authorId);
        }

        cache.Publish(author);

        var authorized = await authorization.AuthorizeAsync(
            user,
            author,
            BookStorePolicies.BooksWrite);

        if (!authorized.Succeeded)
        {
            throw new AuthorNotFoundException(authorId);
        }

        var book = author.AddBook(title, Isbn.Parse(isbn), DateTimeOffset.UtcNow);
        cache.Publish(book);

        await context.SaveChangesAsync(cancellationToken);

        return book;
    }
}
