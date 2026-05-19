using System.Security.Claims;
using GreenDonut.Data;
using Microsoft.AspNetCore.Authorization;
using Mocha.Mediator;
using Reference.GraphQLFirst.Application.Authors.DataLoaders;
using Reference.GraphQLFirst.Application.Books.DataLoaders;
using Reference.GraphQLFirst.Domain.Books;

namespace Reference.GraphQLFirst.Application.Books.Queries;

public sealed record PageBooksByAuthorIdQuery(
    ClaimsPrincipal User,
    Guid AuthorId,
    PagingArguments Paging) : IQuery<Page<Book>?>;

public sealed class PageBooksByAuthorIdQueryHandler(
    IAuthorBatchingContext authors,
    IBookBatchingContext books,
    IAuthorizationService authorization) : IQueryHandler<PageBooksByAuthorIdQuery, Page<Book>?>
{
    public async ValueTask<Page<Book>?> HandleAsync(
        PageBooksByAuthorIdQuery query,
        CancellationToken cancellationToken)
    {
        var (user, authorId, paging) = query;

        if (user.Identity is not { IsAuthenticated: true })
        {
            return null;
        }

        var author = await authors.AuthorById.LoadAsync(authorId, cancellationToken);
        if (author is null)
        {
            return null;
        }

        var result = await authorization.AuthorizeAsync(user, author, "Books.Read");
        if (!result.Succeeded)
        {
            return null;
        }

        return await books
            .PageBooksByAuthorId.With(paging)
            .LoadAsync(authorId, cancellationToken);
    }
}
