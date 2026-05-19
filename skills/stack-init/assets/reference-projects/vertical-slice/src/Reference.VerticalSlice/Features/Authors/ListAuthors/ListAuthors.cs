using Reference.VerticalSlice.Domain;
using Reference.VerticalSlice.Shared.Persistence;
using Reference.VerticalSlice.Shared.Security;

namespace Reference.VerticalSlice.Features.Authors.ListAuthors;

public sealed record ListAuthorsQuery(ClaimsPrincipal User) : IQuery<IReadOnlyList<Author>?>;

public sealed class ListAuthorsQueryHandler(
    AppDbContext context,
    IAuthorizationService authorization)
    : IQueryHandler<ListAuthorsQuery, IReadOnlyList<Author>?>
{
    public async ValueTask<IReadOnlyList<Author>?> HandleAsync(
        ListAuthorsQuery query,
        CancellationToken cancellationToken)
    {
        var user = query.User;

        if (user.Identity is not { IsAuthenticated: true })
        {
            return null;
        }

        var authorized = await authorization.AuthorizeAsync(user, null, LibraryPolicies.Read);
        if (!authorized.Succeeded)
        {
            return null;
        }

        return await context.Authors
            .AsNoTracking()
            .OrderBy(author => author.Name)
            .ThenBy(author => author.Id)
            .ToListAsync(cancellationToken);
    }
}

[QueryType]
public static class ListAuthorsQueryType
{
    public static async ValueTask<IReadOnlyList<Author>?> GetAuthorsAsync(
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        return await sender.QueryAsync(new ListAuthorsQuery(user), cancellationToken);
    }
}
