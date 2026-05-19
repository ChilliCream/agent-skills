using System.Security.Claims;
using HotChocolate.Authorization;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Mocha.Mediator;
using Reference.CleanArchitecture.Application.Authors.Errors;
using Reference.CleanArchitecture.Application.Books.Commands;
using Reference.CleanArchitecture.Application.Books.Errors;
using Reference.CleanArchitecture.Domain.Authors;
using Reference.CleanArchitecture.Domain.Books;

namespace Reference.CleanArchitecture.GraphQL.Books.Operations;

[MutationType]
public sealed class BookMutations
{
    [Authorize]
    [Error<AuthorNotFoundException>]
    [Error<UnauthorizedAccessException>]
    public async ValueTask<Book> AddBookToAuthorAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID<Author>] Guid authorId,
        string title,
        string isbn,
        CancellationToken cancellationToken)
    {
        return await sender.SendAsync(
            new AddBookToAuthorCommand(user, authorId, title, isbn),
            cancellationToken);
    }

    [Authorize]
    [Error<BookNotFoundException>]
    [Error<UnauthorizedAccessException>]
    public async ValueTask<Book> PublishBookAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID<Book>] Guid bookId,
        CancellationToken cancellationToken)
    {
        return await sender.SendAsync(
            new PublishBookCommand(user, bookId),
            cancellationToken);
    }
}
