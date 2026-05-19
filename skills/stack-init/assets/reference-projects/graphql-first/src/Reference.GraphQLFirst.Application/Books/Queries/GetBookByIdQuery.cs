using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Mocha.Mediator;
using Reference.GraphQLFirst.Application.Books.DataLoaders;
using Reference.GraphQLFirst.Domain.Books;

namespace Reference.GraphQLFirst.Application.Books.Queries;

public sealed record GetBookByIdQuery(ClaimsPrincipal User, Guid Id) : IQuery<Book?>;

public sealed class GetBookByIdQueryHandler(
    IBookBatchingContext books,
    IAuthorizationService authorization) : IQueryHandler<GetBookByIdQuery, Book?>
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

        var result = await authorization.AuthorizeAsync(user, book, "Books.Read");
        return result.Succeeded ? book : null;
    }
}
