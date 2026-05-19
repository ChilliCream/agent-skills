using System.Security.Claims;
using Mocha.Mediator;
using Reference.Ddd.Application.Catalog.Products.Queries;
using Reference.Ddd.Catalog.Products;
using Reference.Ddd.Ordering.Orders;

namespace Reference.Ddd.GraphQL.Ordering.Types;

[ObjectType<OrderLine>]
public static partial class OrderLineType
{
    static partial void Configure(IObjectTypeDescriptor<OrderLine> descriptor)
    {
        descriptor.Ignore(x => x.ProductId);
    }

    public static async ValueTask<Product?> GetProductAsync(
        [Parent] OrderLine line,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        return await sender.QueryAsync(new GetProductByIdQuery(user, line.ProductId), cancellationToken);
    }
}
