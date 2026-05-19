using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Mocha.Mediator;
using Reference.GraphQLFirst.Application.Authors.DataLoaders;
using Reference.GraphQLFirst.Domain.Authors;

namespace Reference.GraphQLFirst.Application.Authors.Queries;

public sealed record GetAuthorByIdQuery(ClaimsPrincipal User, Guid Id) : IQuery<Author?>;

public sealed class GetAuthorByIdQueryHandler(
    IAuthorBatchingContext authors,
    IAuthorizationService authorization) : IQueryHandler<GetAuthorByIdQuery, Author?>
{
    public async ValueTask<Author?> HandleAsync(
        GetAuthorByIdQuery query,
        CancellationToken cancellationToken)
    {
        var (user, id) = query;

        if (user.Identity is not { IsAuthenticated: true })
        {
            return null;
        }

        var author = await authors.AuthorById.LoadAsync(id, cancellationToken);
        if (author is null)
        {
            return null;
        }

        var result = await authorization.AuthorizeAsync(user, author, "Authors.Read");
        return result.Succeeded ? author : null;
    }
}
