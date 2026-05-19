using System.Security.Claims;
using GreenDonut.Data;
using HotChocolate.Types.Pagination;
using HotChocolate.Types.Relay;
using Mocha.Mediator;
using Reference.Ddd.Application.Catalog.Products.Queries;
using Reference.Ddd.Catalog.Products;

namespace Reference.Ddd.GraphQL.Catalog.Operations;

[QueryType]
public sealed class ProductQueries
{
    public async ValueTask<Product?> GetProductByIdAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID<Product>] Guid id,
        CancellationToken cancellationToken)
    {
        return await sender.QueryAsync(new GetProductByIdQuery(user, id), cancellationToken);
    }

    [UsePaging]
    public async ValueTask<Connection<Product>?> GetProductsAsync(
        ClaimsPrincipal user,
        ISender sender,
        PagingArguments arguments,
        ProductStatus status = ProductStatus.Active,
        CancellationToken cancellationToken = default)
    {
        var page = await sender.QueryAsync(
            new PageProductsQuery(user, status, arguments),
            cancellationToken);

        if (page is null)
        {
            return null;
        }

        return await Task.FromResult(page).ToConnectionAsync();
    }
}
