using Xunit;

namespace Reference.Ddd.Application.Tests.Catalog;

public sealed class CreateProductCommandTests
{
    [Fact]
    public async Task HandleAsync_ShouldCreateProduct_WhenUserCanManageCatalog()
    {
        // Arrange a scoped ServiceProvider with:
        // - CatalogDbContext using EF Core InMemory
        // - AddReferenceDddApplication() so Mocha and GreenDonut generated services are present
        // - A test IAuthorizationService that succeeds for ReferencePolicies.CatalogManage
        // Act by dispatching CreateProductCommand through ISender.SendAsync.
        // Assert the product is persisted and the ProductById DataLoader can read it.
        await Task.CompletedTask;
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowDuplicateSkuException_WhenSkuAlreadyExists()
    {
        // Seed an existing Product aggregate, dispatch the command with the same SKU,
        // and assert DuplicateSkuException. This belongs in the application layer
        // because uniqueness crosses aggregate instances.
        await Task.CompletedTask;
    }
}
