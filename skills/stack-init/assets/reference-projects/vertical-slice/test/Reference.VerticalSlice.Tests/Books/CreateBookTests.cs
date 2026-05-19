using GreenDonut;
using Reference.VerticalSlice.Domain;
using Reference.VerticalSlice.Features.Authors;
using Reference.VerticalSlice.Features.Books.CreateBook;
using Reference.VerticalSlice.Tests.Support;

namespace Reference.VerticalSlice.Tests.Books;

public sealed class CreateBookTests
{
    [Fact]
    public async Task HandleAsync_ShouldCreateBook_WhenAuthorExists()
    {
        await using var context = DbContextFactory.Create();
        var author = Author.Create("Martha Wells");
        context.Authors.Add(author);
        await context.SaveChangesAsync();

        var handler = new CreateBookCommandHandler(
            context,
            new PromiseCache(128),
            new TestAuthorizationService());

        var book = await handler.HandleAsync(
            new CreateBookCommand(TestUsers.Authenticated(), author.Id, "  All Systems Red  "),
            CancellationToken.None);

        Assert.Equal(author.Id, book.AuthorId);
        Assert.Equal("All Systems Red", book.Title);
        Assert.Single(context.Books);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowAuthorNotFoundException_WhenAuthorDoesNotExist()
    {
        await using var context = DbContextFactory.Create();
        var handler = new CreateBookCommandHandler(
            context,
            new PromiseCache(128),
            new TestAuthorizationService());

        await Assert.ThrowsAsync<AuthorNotFoundException>(async () =>
            await handler.HandleAsync(
                new CreateBookCommand(TestUsers.Authenticated(), Guid.CreateVersion7(), "All Systems Red"),
                CancellationToken.None));
    }
}
