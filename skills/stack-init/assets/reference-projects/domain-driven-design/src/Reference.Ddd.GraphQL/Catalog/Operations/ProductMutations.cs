using System.Security.Claims;
using HotChocolate.Authorization;
using HotChocolate.Types.Relay;
using Mocha.Mediator;
using Reference.Ddd.Application.Catalog.Products.Commands;
using Reference.Ddd.Application.Catalog.Products.Errors;
using Reference.Ddd.Catalog.Products;

namespace Reference.Ddd.GraphQL.Catalog.Operations;

[MutationType]
public sealed class ProductMutations
{
    [Authorize]
    [Error<DuplicateSkuException>]
    [Error<UnauthorizedAccessException>]
    public async ValueTask<Product> CreateProductAsync(
        ClaimsPrincipal user,
        ISender sender,
        string sku,
        string name,
        string? description,
        decimal priceAmount,
        string currency,
        CancellationToken cancellationToken)
    {
        return await sender.SendAsync(
            new CreateProductCommand(user, sku, name, description, priceAmount, currency),
            cancellationToken);
    }

    [Authorize]
    [Error<ProductNotFoundException>]
    [Error<UnauthorizedAccessException>]
    public async ValueTask<Product> ChangeProductPriceAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID<Product>] Guid productId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken)
    {
        return await sender.SendAsync(
            new ChangeProductPriceCommand(user, productId, amount, currency),
            cancellationToken);
    }
}
