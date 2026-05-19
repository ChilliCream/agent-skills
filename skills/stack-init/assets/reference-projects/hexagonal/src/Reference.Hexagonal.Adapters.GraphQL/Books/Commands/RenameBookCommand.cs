using System.Security.Claims;
using GreenDonut;
using Mocha.Mediator;
using Reference.Hexagonal.Adapters.GraphQL.Security;
using Reference.Hexagonal.Core.Domain;
using Reference.Hexagonal.Core.Ports.In;

namespace Reference.Hexagonal.Adapters.GraphQL.Books.Commands;

public sealed record RenameBookCommand(
    ClaimsPrincipal User,
    Guid BookId,
    string Title) : ICommand<Book>;

public sealed class RenameBookCommandHandler(
    IRenameBook renameBook,
    IPromiseCache cache) : ICommandHandler<RenameBookCommand, Book>
{
    public async ValueTask<Book> HandleAsync(
        RenameBookCommand command,
        CancellationToken cancellationToken)
    {
        AuthenticatedUser.EnsureAuthenticated(command.User);

        var book = await renameBook.HandleAsync(
            new RenameBookInput(command.BookId, command.Title),
            cancellationToken);

        cache.Publish(book);

        return book;
    }
}
