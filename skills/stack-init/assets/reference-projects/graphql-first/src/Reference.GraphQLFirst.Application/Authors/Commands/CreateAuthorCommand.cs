using System.Security.Claims;
using GreenDonut;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Mocha.Mediator;
using Reference.GraphQLFirst.Application.Abstractions;
using Reference.GraphQLFirst.Application.Authors.Errors;
using Reference.GraphQLFirst.Domain.Authors;

namespace Reference.GraphQLFirst.Application.Authors.Commands;

public sealed record CreateAuthorCommand(ClaimsPrincipal User, string Name) : ICommand<Author>;

public sealed class CreateAuthorCommandHandler(
    IPromiseCache cache,
    IReferenceDbContext context,
    IAuthorizationService authorization) : ICommandHandler<CreateAuthorCommand, Author>
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

        var createAuthorization = await authorization.AuthorizeAsync(
            user,
            resource: null,
            policyName: "Authors.Create");

        if (!createAuthorization.Succeeded)
        {
            throw new UnauthorizedAccessException();
        }

        var normalizedName = Author.NormalizeName(name);
        var exists = await context.Authors
            .AnyAsync(x => x.Name == normalizedName, cancellationToken);

        if (exists)
        {
            throw new DuplicateAuthorNameException(normalizedName);
        }

        var author = Author.Create(normalizedName, DateTimeOffset.UtcNow);

        context.Authors.Add(author);
        cache.Publish(author);
        await context.SaveChangesAsync(cancellationToken);

        return author;
    }
}
