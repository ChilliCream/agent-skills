using System.Security.Claims;
using GreenDonut;
using Mocha.Mediator;
using Reference.Hexagonal.Adapters.GraphQL.Security;
using Reference.Hexagonal.Core.Domain;
using Reference.Hexagonal.Core.Ports.In;

namespace Reference.Hexagonal.Adapters.GraphQL.Authors.Commands;

public sealed record CreateAuthorCommand(
    ClaimsPrincipal User,
    string Name,
    string? Biography) : ICommand<Author>;

public sealed class CreateAuthorCommandHandler(
    ICreateAuthor createAuthor,
    IPromiseCache cache) : ICommandHandler<CreateAuthorCommand, Author>
{
    public async ValueTask<Author> HandleAsync(
        CreateAuthorCommand command,
        CancellationToken cancellationToken)
    {
        AuthenticatedUser.EnsureAuthenticated(command.User);

        var author = await createAuthor.HandleAsync(
            new CreateAuthorInput(command.Name, command.Biography),
            cancellationToken);

        cache.Publish(author);

        return author;
    }
}
