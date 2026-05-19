using Xunit;

namespace Reference.GraphQLFirst.Application.Tests.Books;

public sealed class CreateBookCommandTests
{
    [Fact(Skip = "Reference skeleton: wire Mocha, GreenDonut generated contexts, IPromiseCache, and EF InMemory in the consuming repo.")]
    public Task HandleAsync_ShouldCreateBook_WhenAuthorExistsAndUserCanCreateBooks()
        => Task.CompletedTask;

    [Fact(Skip = "Reference skeleton: assert UnauthorizedAccessException for anonymous users.")]
    public Task HandleAsync_ShouldThrowUnauthorizedAccessException_WhenNotAuthenticated()
        => Task.CompletedTask;

    [Fact(Skip = "Reference skeleton: assert AuthorNotFoundException for missing author and denied author access.")]
    public Task HandleAsync_ShouldThrowAuthorNotFoundException_WhenAuthorCannotBeUsed()
        => Task.CompletedTask;
}
