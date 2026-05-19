using Xunit;
using Microsoft.EntityFrameworkCore;
using Reference.Hexagonal.Adapters.Persistence;
using Reference.Hexagonal.Core.Domain;

namespace Reference.Hexagonal.Adapters.Persistence.Tests;

public sealed class LibraryDbContextTests
{
    [Fact]
    public async Task SaveChangesAsync_ShouldPersistAuthorAndBook()
    {
        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;

        await using var context = new LibraryDbContext(options);
        var author = Author.Create("N. K. Jemisin", null);
        var book = Book.Register(
            author.Id,
            Isbn.Parse("9780316229296"),
            "The Fifth Season",
            null,
            new DateOnly(2015, 8, 4));

        context.Authors.Add(author);
        context.Books.Add(book);
        await context.SaveChangesAsync();

        var loaded = await context.Books.SingleAsync(x => x.Id == book.Id);
        Assert.Equal(author.Id, loaded.AuthorId);
        Assert.Equal("9780316229296", loaded.Isbn.Value);
    }

    [Fact(Skip = "Skeleton: build a request service scope and assert DataLoader batching after the host selects its real database provider.")]
    public Task DataLoaders_ShouldBatchBookAndAuthorLookups()
        => Task.CompletedTask;
}
