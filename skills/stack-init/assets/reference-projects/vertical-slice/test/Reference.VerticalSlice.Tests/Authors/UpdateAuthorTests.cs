using GreenDonut;
using Reference.VerticalSlice.Domain;
using Reference.VerticalSlice.Features.Authors;
using Reference.VerticalSlice.Features.Authors.UpdateAuthor;
using Reference.VerticalSlice.Tests.Support;

namespace Reference.VerticalSlice.Tests.Authors;

public sealed class UpdateAuthorTests
{
    [Fact]
    public async Task HandleAsync_ShouldRenameAuthor_WhenAuthorized()
    {
        await using var context = DbContextFactory.Create();
        var author = Author.Create("Ursula Le Guin");

        context.Authors.Add(author);
        await context.SaveChangesAsync();

        var handler = new UpdateAuthorNameCommandHandler(
            context,
            new PromiseCache(128),
            new TestAuthorizationService());

        var updated = await handler.HandleAsync(
            new UpdateAuthorNameCommand(TestUsers.Authenticated(), author.Id, "Ursula K. Le Guin"),
            CancellationToken.None);

        Assert.Equal("Ursula K. Le Guin", updated.Name);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowAuthorNotFoundException_WhenPermissionFails()
    {
        await using var context = DbContextFactory.Create();
        var author = Author.Create("Ursula K. Le Guin");

        context.Authors.Add(author);
        await context.SaveChangesAsync();

        var handler = new UpdateAuthorNameCommandHandler(
            context,
            new PromiseCache(128),
            new TestAuthorizationService(allow: false));

        await Assert.ThrowsAsync<AuthorNotFoundException>(async () =>
            await handler.HandleAsync(
                new UpdateAuthorNameCommand(TestUsers.Authenticated(), author.Id, "Ursula Le Guin"),
                CancellationToken.None));
    }
}
