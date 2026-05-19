using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Reference.CleanArchitecture.Application.Authors.DataLoaders;
using Reference.CleanArchitecture.Application.Common;
using Reference.CleanArchitecture.Domain.Authors;
using Mocha.Mediator;

namespace Reference.CleanArchitecture.Application.Authors.Queries;

public sealed record GetAuthorByIdQuery(ClaimsPrincipal User, Guid Id) : IQuery<Author?>;

public sealed class GetAuthorByIdQueryHandler(
    IAuthorBatchingContext authors,
    IAuthorizationService authorization)
    : IQueryHandler<GetAuthorByIdQuery, Author?>
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

        var authorized = await authorization.AuthorizeAsync(
            user,
            author,
            BookStorePolicies.AuthorsRead);

        return authorized.Succeeded ? author : null;
    }
}
