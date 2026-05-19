using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reference.CleanArchitecture.Application.Authors.DataLoaders;
using Reference.CleanArchitecture.Application.Books.DataLoaders;
using Reference.CleanArchitecture.Application.Common;
using Reference.CleanArchitecture.Domain.Books;
using Mocha.Mediator;

namespace Reference.CleanArchitecture.Application.Books.Queries;

public sealed record GetBooksByAuthorIdQuery(ClaimsPrincipal User, Guid AuthorId)
    : IQuery<IReadOnlyList<Book>?>;

public sealed class GetBooksByAuthorIdQueryHandler(
    IAuthorBatchingContext authors,
    IBookBatchingContext books,
    IAuthorizationService authorization)
    : IQueryHandler<GetBooksByAuthorIdQuery, IReadOnlyList<Book>?>
{
    public async ValueTask<IReadOnlyList<Book>?> HandleAsync(
        GetBooksByAuthorIdQuery query,
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

        var authorized = await authorization.AuthorizeAsync(
            user,
            author,
            BookStorePolicies.BooksRead);

        if (!authorized.Succeeded)
        {
            return null;
        }

        var authorBooks = await books.BooksByAuthorId.LoadAsync(authorId, cancellationToken)
            ?? [];
        return authorBooks.ToArray();
    }
}
