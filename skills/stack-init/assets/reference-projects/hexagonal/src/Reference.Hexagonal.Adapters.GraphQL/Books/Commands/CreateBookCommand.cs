using System.Security.Claims;
using GreenDonut;
using Mocha.Mediator;
using Reference.Hexagonal.Adapters.GraphQL.Security;
using Reference.Hexagonal.Core.Domain;
using Reference.Hexagonal.Core.Ports.In;

namespace Reference.Hexagonal.Adapters.GraphQL.Books.Commands;

public sealed record CreateBookCommand(
    ClaimsPrincipal User,
    Guid AuthorId,
    string Isbn,
    string Title,
    string? Synopsis,
    DateOnly? PublishedOn) : ICommand<Book>;

public sealed class CreateBookCommandHandler(
    IRegisterBook registerBook,
    IPromiseCache cache) : ICommandHandler<CreateBookCommand, Book>
{
    public async ValueTask<Book> HandleAsync(
        CreateBookCommand command,
        CancellationToken cancellationToken)
    {
        AuthenticatedUser.EnsureAuthenticated(command.User);

        var book = await registerBook.HandleAsync(
            new RegisterBookInput(
                command.AuthorId,
                command.Isbn,
                command.Title,
                command.Synopsis,
                command.PublishedOn),
            cancellationToken);

        cache.Publish(book);

        return book;
    }
}
