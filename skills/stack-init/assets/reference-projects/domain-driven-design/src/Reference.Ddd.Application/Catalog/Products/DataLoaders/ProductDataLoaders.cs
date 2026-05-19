using GreenDonut;
using GreenDonut.Data;
using Microsoft.EntityFrameworkCore;
using Reference.Ddd.Application.Catalog.Abstractions;
using Reference.Ddd.Catalog.Products;

namespace Reference.Ddd.Application.Catalog.Products.DataLoaders;

[DataLoaderGroup("ProductBatchingContext")]
public static class ProductDataLoaders
{
    [DataLoader(Lookups = [nameof(GetProductByIdLookup)])]
    public static async Task<Dictionary<Guid, Product>> GetProductByIdAsync(
        IReadOnlyList<Guid> keys,
        ICatalogDbContext context,
        CancellationToken cancellationToken)
    {
        return await context.Products
            .AsNoTracking()
            .Where(x => keys.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
    }

    public static Guid GetProductByIdLookup(Product product) => product.Id;

    [DataLoader]
    public static async Task<Dictionary<ProductStatus, Page<Product>>> PageProductsByStatusAsync(
        IReadOnlyList<ProductStatus> keys,
        PagingArguments arguments,
        ICatalogDbContext context,
        CancellationToken cancellationToken)
    {
        return await context.Products
            .AsNoTracking()
            .Where(x => keys.Contains(x.Status))
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .ToBatchPageAsync(x => x.Status, arguments, cancellationToken);
    }
}
