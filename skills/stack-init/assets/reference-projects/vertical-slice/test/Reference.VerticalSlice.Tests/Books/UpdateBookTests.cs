using GreenDonut;
using Reference.VerticalSlice.Domain;
using Reference.VerticalSlice.Features.Books;
using Reference.VerticalSlice.Features.Books.UpdateBook;
using Reference.VerticalSlice.Tests.Support;

namespace Reference.VerticalSlice.Tests.Books;

public sealed class UpdateBookTests
{
    [Fact]
    public async Task HandleAsync_ShouldRenameBook_WhenAuthorized()
    {
        await using var context = DbContextFactory.Create();
        var author = Author.Create("Lois McMaster Bujold");
        var book = Book.Create(author.Id, "Shards of Honour");

        context.Authors.Add(author);
        context.Books.Add(book);
        await context.SaveChangesAsync();

        var handler = new UpdateBookTitleCommandHandler(
            context,
            new PromiseCache(128),
            new TestAuthorizationService());

        var updated = await handler.HandleAsync(
            new UpdateBookTitleCommand(TestUsers.Authenticated(), book.Id, "Shards of Honor"),
            CancellationToken.None);

        Assert.Equal("Shards of Honor", updated.Title);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowBookNotFoundException_WhenPermissionFails()
    {
        await using var context = DbContextFactory.Create();
        var author = Author.Create("Lois McMaster Bujold");
        var book = Book.Create(author.Id, "Barrayar");

        context.Authors.Add(author);
        context.Books.Add(book);
        await context.SaveChangesAsync();

        var handler = new UpdateBookTitleCommandHandler(
            context,
            new PromiseCache(128),
            new TestAuthorizationService(allow: false));

        await Assert.ThrowsAsync<BookNotFoundException>(async () =>
            await handler.HandleAsync(
                new UpdateBookTitleCommand(TestUsers.Authenticated(), book.Id, "Barrayar"),
                CancellationToken.None));
    }
}
