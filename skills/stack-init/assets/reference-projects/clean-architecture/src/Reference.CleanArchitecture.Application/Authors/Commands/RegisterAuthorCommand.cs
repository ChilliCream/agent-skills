using System.Security.Claims;
using GreenDonut;
using Microsoft.AspNetCore.Authorization;
using Reference.CleanArchitecture.Application.Common;
using Reference.CleanArchitecture.Domain.Authors;
using Mocha.Mediator;

namespace Reference.CleanArchitecture.Application.Authors.Commands;

public sealed record RegisterAuthorCommand(ClaimsPrincipal User, string Name) : ICommand<Author>;

public sealed class RegisterAuthorCommandHandler(
    IAppDbContext context,
    IPromiseCache cache,
    IAuthorizationService authorization)
    : ICommandHandler<RegisterAuthorCommand, Author>
{
    public async ValueTask<Author> HandleAsync(
        RegisterAuthorCommand command,
        CancellationToken cancellationToken)
    {
        var (user, name) = command;

        if (user.Identity is not { IsAuthenticated: true })
        {
            throw new UnauthorizedAccessException();
        }

        var authorized = await authorization.AuthorizeAsync(
            user,
            null,
            BookStorePolicies.AuthorsWrite);

        if (!authorized.Succeeded)
        {
            throw new UnauthorizedAccessException();
        }

        var author = Author.Register(new AuthorName(name), DateTimeOffset.UtcNow);

        context.Authors.Add(author);
        cache.Publish(author);

        await context.SaveChangesAsync(cancellationToken);

        return author;
    }
}
