using Reference.VerticalSlice.Domain;
using Reference.VerticalSlice.Features.Authors;
using Reference.VerticalSlice.Shared.Persistence;
using Reference.VerticalSlice.Shared.Security;

namespace Reference.VerticalSlice.Features.Authors.UpdateAuthor;

public sealed record UpdateAuthorNameCommand(
    ClaimsPrincipal User,
    Guid AuthorId,
    string Name) : ICommand<Author>;

public sealed class UpdateAuthorNameCommandHandler(
    AppDbContext context,
    IPromiseCache cache,
    IAuthorizationService authorization)
    : ICommandHandler<UpdateAuthorNameCommand, Author>
{
    public async ValueTask<Author> HandleAsync(
        UpdateAuthorNameCommand command,
        CancellationToken cancellationToken)
    {
        var (user, authorId, name) = command;

        if (user.Identity is not { IsAuthenticated: true })
        {
            throw new UnauthorizedAccessException();
        }

        var author = await context.Authors
            .FirstOrDefaultAsync(candidate => candidate.Id == authorId, cancellationToken);

        if (author is null)
        {
            throw new AuthorNotFoundException(authorId);
        }

        cache.Publish(author);

        var authorized = await authorization.AuthorizeAsync(user, author, LibraryPolicies.Write);
        if (!authorized.Succeeded)
        {
            throw new AuthorNotFoundException(authorId);
        }

        author.Rename(name);
        cache.Publish(author);

        await context.SaveChangesAsync(cancellationToken);
        return author;
    }
}

[MutationType]
public static class UpdateAuthorMutation
{
    [HotChocolate.Authorization.Authorize]
    [Error<AuthorNotFoundException>]
    [Error<UnauthorizedAccessException>]
    [Error<ArgumentException>]
    public static async ValueTask<Author> UpdateAuthorNameAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID(nameof(Author))] Guid authorId,
        string name,
        CancellationToken cancellationToken)
    {
        return await sender.SendAsync(
            new UpdateAuthorNameCommand(user, authorId, name),
            cancellationToken);
    }
}
