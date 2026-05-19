using Reference.VerticalSlice.Domain;
using Reference.VerticalSlice.Features.Authors.DataLoaders;
using Reference.VerticalSlice.Shared.Security;

namespace Reference.VerticalSlice.Features.Authors.GetAuthorById;

public sealed record GetAuthorByIdQuery(ClaimsPrincipal User, Guid AuthorId) : IQuery<Author?>;

public sealed class GetAuthorByIdQueryHandler(
    IAuthorBatchingContext authors,
    IAuthorizationService authorization)
    : IQueryHandler<GetAuthorByIdQuery, Author?>
{
    public async ValueTask<Author?> HandleAsync(
        GetAuthorByIdQuery query,
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

        var authorized = await authorization.AuthorizeAsync(user, author, LibraryPolicies.Read);
        return authorized.Succeeded ? author : null;
    }
}

[QueryType]
public static class GetAuthorByIdQueryType
{
    public static async ValueTask<Author?> GetAuthorByIdAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID(nameof(Author))] Guid authorId,
        CancellationToken cancellationToken)
    {
        return await sender.QueryAsync(
            new GetAuthorByIdQuery(user, authorId),
            cancellationToken);
    }
}
