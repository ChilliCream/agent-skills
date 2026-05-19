using System.Security.Claims;
using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Mocha.Mediator;
using Reference.GraphQLFirst.Application.Authors.Errors;
using Reference.GraphQLFirst.Application.Books.Commands;
using Reference.GraphQLFirst.Application.Books.Errors;
using Reference.GraphQLFirst.Domain.Authors;
using Reference.GraphQLFirst.Domain.Books;
using Reference.GraphQLFirst.Domain.Common;

namespace Reference.GraphQLFirst.GraphQL.Books.Operations;

[MutationType]
public static partial class BookMutations
{
    [Authorize]
    [Error<AuthorNotFoundException>]
    [Error<DuplicateBookIsbnException>]
    [Error<DomainValidationException>]
    [Error<UnauthorizedAccessException>]
    public static async ValueTask<Book> CreateBookAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID<Author>] Guid authorId,
        string title,
        int? publicationYear,
        string? isbn,
        CancellationToken cancellationToken)
        => await sender.SendAsync(
            new CreateBookCommand(user, authorId, title, publicationYear, isbn),
            cancellationToken);
}
