using Reference.VerticalSlice.Domain;
using Reference.VerticalSlice.Features.Books.DataLoaders;
using Reference.VerticalSlice.Tests.Support;

namespace Reference.VerticalSlice.Tests.Books;

public sealed class BookDataLoaderTests
{
    [Fact]
    public async Task GetBooksByAuthorIdAsync_ShouldGroupBooksByAuthor()
    {
        await using var context = DbContextFactory.Create();
        var firstAuthor = Author.Create("Ann Leckie");
        var secondAuthor = Author.Create("Becky Chambers");

        context.Authors.AddRange(firstAuthor, secondAuthor);
        context.Books.AddRange(
            Book.Create(firstAuthor.Id, "Ancillary Justice"),
            Book.Create(firstAuthor.Id, "Ancillary Sword"),
            Book.Create(secondAuthor.Id, "The Long Way to a Small Angry Planet"));
        await context.SaveChangesAsync();

        var lookup = await BookDataLoaders.GetBooksByAuthorIdAsync(
            [firstAuthor.Id, secondAuthor.Id],
            context,
            CancellationToken.None);

        Assert.Equal(2, lookup[firstAuthor.Id].Count());
        Assert.Single(lookup[secondAuthor.Id]);
    }
}
