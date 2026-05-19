using System.Security.Claims;
using GreenDonut.Data;
using HotChocolate.Types.Pagination;
using HotChocolate.Types.Relay;
using Mocha.Mediator;
using Reference.Ddd.Application.Ordering.Orders.Queries;
using Reference.Ddd.Ordering.Orders;

namespace Reference.Ddd.GraphQL.Ordering.Operations;

[QueryType]
public sealed class OrderQueries
{
    public async ValueTask<Order?> GetOrderByIdAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID<Order>] Guid id,
        CancellationToken cancellationToken)
    {
        return await sender.QueryAsync(new GetOrderByIdQuery(user, id), cancellationToken);
    }

    [UsePaging]
    public async ValueTask<Connection<Order>?> GetOrdersByCustomerIdAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID("Customer")] Guid customerId,
        PagingArguments arguments,
        CancellationToken cancellationToken)
    {
        var page = await sender.QueryAsync(
            new PageOrdersByCustomerIdQuery(user, customerId, arguments),
            cancellationToken);

        if (page is null)
        {
            return null;
        }

        return await Task.FromResult(page).ToConnectionAsync();
    }
}
