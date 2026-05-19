using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reference.CleanArchitecture.Application.Books.DataLoaders;
using Reference.CleanArchitecture.Application.Common;
using Reference.CleanArchitecture.Domain.Books;
using Mocha.Mediator;

namespace Reference.CleanArchitecture.Application.Books.Queries;

public sealed record GetBookByIdQuery(ClaimsPrincipal User, Guid Id) : IQuery<Book?>;

public sealed class GetBookByIdQueryHandler(
    IBookBatchingContext books,
    IAuthorizationService authorization)
    : IQueryHandler<GetBookByIdQuery, Book?>
{
    public async ValueTask<Book?> HandleAsync(
        GetBookByIdQuery query,
        CancellationToken cancellationToken)
    {
        var (user, id) = query;

        if (user.Identity is not { IsAuthenticated: true })
        {
            return null;
        }

        var book = await books.BookById.LoadAsync(id, cancellationToken);
        if (book is null)
        {
            return null;
        }

        var authorized = await authorization.AuthorizeAsync(
            user,
            book,
            BookStorePolicies.BooksRead);

        return authorized.Succeeded ? book : null;
    }
}
