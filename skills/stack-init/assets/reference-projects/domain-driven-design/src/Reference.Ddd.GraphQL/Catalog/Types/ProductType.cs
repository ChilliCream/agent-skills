using System.Security.Claims;
using HotChocolate.Types.Relay;
using Mocha.Mediator;
using Reference.Ddd.Application.Catalog.Products.Queries;
using Reference.Ddd.Catalog.Products;

namespace Reference.Ddd.GraphQL.Catalog.Types;

[ObjectType<Product>]
public static partial class ProductType
{
    static partial void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Ignore(x => x.DomainEvents);
    }

    [NodeResolver]
    public static async ValueTask<Product?> GetProductByIdAsync(
        Guid id,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        return await sender.QueryAsync(new GetProductByIdQuery(user, id), cancellationToken);
    }

    [BindMember(nameof(Product.Sku))]
    public static string GetSku([Parent] Product product) => product.Sku.Value;
}
