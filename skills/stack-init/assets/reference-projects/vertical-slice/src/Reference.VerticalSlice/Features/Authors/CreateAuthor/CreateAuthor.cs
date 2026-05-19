using Reference.VerticalSlice.Domain;
using Reference.VerticalSlice.Shared.Persistence;
using Reference.VerticalSlice.Shared.Security;

namespace Reference.VerticalSlice.Features.Authors.CreateAuthor;

public sealed record CreateAuthorCommand(ClaimsPrincipal User, string Name) : ICommand<Author>;

public sealed class CreateAuthorCommandHandler(
    AppDbContext context,
    IPromiseCache cache,
    IAuthorizationService authorization)
    : ICommandHandler<CreateAuthorCommand, Author>
{
    public async ValueTask<Author> HandleAsync(
        CreateAuthorCommand command,
        CancellationToken cancellationToken)
    {
        var (user, name) = command;

        if (user.Identity is not { IsAuthenticated: true })
        {
            throw new UnauthorizedAccessException();
        }

        var authorized = await authorization.AuthorizeAsync(user, null, LibraryPolicies.Write);
        if (!authorized.Succeeded)
        {
            throw new UnauthorizedAccessException();
        }

        var author = Author.Create(name);

        context.Authors.Add(author);
        cache.Publish(author);
        await context.SaveChangesAsync(cancellationToken);

        return author;
    }
}

[MutationType]
public static class CreateAuthorMutation
{
    [HotChocolate.Authorization.Authorize]
    [Error<UnauthorizedAccessException>]
    [Error<ArgumentException>]
    public static async ValueTask<Author> CreateAuthorAsync(
        ClaimsPrincipal user,
        ISender sender,
        string name,
        CancellationToken cancellationToken)
    {
        return await sender.SendAsync(
            new CreateAuthorCommand(user, name),
            cancellationToken);
    }
}
