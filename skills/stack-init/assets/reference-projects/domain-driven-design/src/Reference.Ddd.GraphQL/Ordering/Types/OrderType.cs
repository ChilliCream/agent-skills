using System.Security.Claims;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Mocha.Mediator;
using Reference.Ddd.Application.Ordering.Orders.Queries;
using Reference.Ddd.Ordering.Orders;

namespace Reference.Ddd.GraphQL.Ordering.Types;

[ObjectType<Order>]
public static partial class OrderType
{
    static partial void Configure(IObjectTypeDescriptor<Order> descriptor)
    {
        descriptor.Ignore(x => x.DomainEvents);
        descriptor.Field(x => x.CustomerId).Type<NonNullType<IdType>>();
    }

    [NodeResolver]
    public static async ValueTask<Order?> GetOrderByIdAsync(
        Guid id,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        return await sender.QueryAsync(new GetOrderByIdQuery(user, id), cancellationToken);
    }
}
