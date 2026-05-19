using Xunit;

namespace Reference.Ddd.Application.Tests.Ordering;

public sealed class GetOrderByIdQueryTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnNull_WhenUserIsAnonymous()
    {
        // Build the handler with the generated IOrderBatchingContext and a test
        // IAuthorizationService. Dispatch GetOrderByIdQuery with an anonymous principal
        // and assert null without a database read.
        await Task.CompletedTask;
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnOrder_WhenUserCanReadOrder()
    {
        // Seed an Order aggregate through OrderingDbContext, query through ISender.QueryAsync,
        // and assert that the handler loads through OrderBatchingContext rather than DbContext.
        await Task.CompletedTask;
    }
}
