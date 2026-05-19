using System.Security.Claims;
using GreenDonut;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Mocha.Mediator;
using Reference.GraphQLFirst.Application.Abstractions;
using Reference.GraphQLFirst.Application.Authors.Errors;
using Reference.GraphQLFirst.Application.Books.Errors;
using Reference.GraphQLFirst.Domain.Books;

namespace Reference.GraphQLFirst.Application.Books.Commands;

public sealed record CreateBookCommand(
    ClaimsPrincipal User,
    Guid AuthorId,
    string Title,
    int? PublicationYear,
    string? Isbn) : ICommand<Book>;

public sealed class CreateBookCommandHandler(
    IPromiseCache cache,
    IReferenceDbContext context,
    IAuthorizationService authorization) : ICommandHandler<CreateBookCommand, Book>
{
    public async ValueTask<Book> HandleAsync(
        CreateBookCommand command,
        CancellationToken cancellationToken)
    {
        var (user, authorId, title, publicationYear, isbn) = command;

        if (user.Identity is not { IsAuthenticated: true })
        {
            throw new UnauthorizedAccessException();
        }

        var author = await context.Authors
            .FirstOrDefaultAsync(x => x.Id == authorId, cancellationToken);

        if (author is null)
        {
            throw new AuthorNotFoundException(authorId);
        }

        cache.Publish(author);

        var authorizationResult = await authorization.AuthorizeAsync(user, author, "Books.Create");
        if (!authorizationResult.Succeeded)
        {
            throw new AuthorNotFoundException(authorId);
        }

        var normalizedIsbn = Book.NormalizeIsbn(isbn);
        if (normalizedIsbn is not null)
        {
            var isbnExists = await context.Books
                .AnyAsync(x => x.Isbn == normalizedIsbn, cancellationToken);

            if (isbnExists)
            {
                throw new DuplicateBookIsbnException(normalizedIsbn);
            }
        }

        var book = Book.Create(
            author.Id,
            title,
            publicationYear,
            normalizedIsbn,
            DateTimeOffset.UtcNow);

        context.Books.Add(book);
        cache.Publish(book);
        await context.SaveChangesAsync(cancellationToken);

        return book;
    }
}
