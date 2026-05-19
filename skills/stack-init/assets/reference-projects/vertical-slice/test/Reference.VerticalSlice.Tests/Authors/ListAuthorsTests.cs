using Reference.VerticalSlice.Domain;
using Reference.VerticalSlice.Features.Authors.ListAuthors;
using Reference.VerticalSlice.Tests.Support;

namespace Reference.VerticalSlice.Tests.Authors;

public sealed class ListAuthorsTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnAuthorsOrderedByName_WhenAuthorized()
    {
        await using var context = DbContextFactory.Create();
        context.Authors.AddRange(Author.Create("Zadie Smith"), Author.Create("Ada Palmer"));
        await context.SaveChangesAsync();

        var handler = new ListAuthorsQueryHandler(context, new TestAuthorizationService());

        var authors = await handler.HandleAsync(
            new ListAuthorsQuery(TestUsers.Authenticated()),
            CancellationToken.None);

        Assert.NotNull(authors);
        Assert.Collection(
            authors,
            first => Assert.Equal("Ada Palmer", first.Name),
            second => Assert.Equal("Zadie Smith", second.Name));
    }
}
