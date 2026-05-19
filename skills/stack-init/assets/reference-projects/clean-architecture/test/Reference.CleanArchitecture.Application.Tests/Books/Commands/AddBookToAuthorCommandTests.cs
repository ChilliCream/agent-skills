using System.Security.Claims;
using Reference.CleanArchitecture.Application.Books.Commands;
using Xunit;

namespace Reference.CleanArchitecture.Application.Tests.Books.Commands;

public sealed class AddBookToAuthorCommandTests
{
    [Fact]
    public void Command_ShouldCarryCurrentUserAndInput()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity("test"));
        var authorId = Guid.NewGuid();

        var command = new AddBookToAuthorCommand(
            user,
            authorId,
            "Domain-Driven Design",
            "9780321125217");

        Assert.Same(user, command.User);
        Assert.Equal(authorId, command.AuthorId);
        Assert.Equal("Domain-Driven Design", command.Title);
        Assert.Equal("9780321125217", command.Isbn);
    }

    [Fact(Skip = "Reference skeleton: in a target repo, wire EF InMemory, IPromiseCache, and IAuthorizationService, then assert the handler order.")]
    public Task HandleAsync_ShouldAddBook_WhenUserCanWriteAuthor()
        => Task.CompletedTask;
}
