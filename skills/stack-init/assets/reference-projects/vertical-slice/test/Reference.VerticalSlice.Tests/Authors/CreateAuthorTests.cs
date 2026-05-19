using GreenDonut;
using Reference.VerticalSlice.Features.Authors.CreateAuthor;
using Reference.VerticalSlice.Tests.Support;

namespace Reference.VerticalSlice.Tests.Authors;

public sealed class CreateAuthorTests
{
    [Fact]
    public async Task HandleAsync_ShouldCreateAuthor_WhenInputIsValid()
    {
        await using var context = DbContextFactory.Create();
        var handler = new CreateAuthorCommandHandler(
            context,
            new PromiseCache(128),
            new TestAuthorizationService());

        var author = await handler.HandleAsync(
            new CreateAuthorCommand(TestUsers.Authenticated(), "  N. K. Jemisin  "),
            CancellationToken.None);

        Assert.Equal("N. K. Jemisin", author.Name);
        Assert.Single(context.Authors);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowUnauthorizedAccessException_WhenNotAuthenticated()
    {
        await using var context = DbContextFactory.Create();
        var handler = new CreateAuthorCommandHandler(
            context,
            new PromiseCache(128),
            new TestAuthorizationService());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await handler.HandleAsync(
                new CreateAuthorCommand(TestUsers.Anonymous(), "N. K. Jemisin"),
                CancellationToken.None));
    }
}
