using System.Security.Claims;
using HotChocolate.Authorization;
using HotChocolate.Types.Relay;
using Mocha.Mediator;
using Reference.Ddd.Application.Ordering.Orders.Commands;
using Reference.Ddd.Application.Ordering.Orders.Errors;
using Reference.Ddd.Catalog.Products;
using Reference.Ddd.Ordering.Orders;

namespace Reference.Ddd.GraphQL.Ordering.Operations;

[MutationType]
public sealed class OrderMutations
{
    [Authorize]
    [Error<UnauthorizedAccessException>]
    public async ValueTask<Order> StartOrderAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID("Customer")] Guid customerId,
        string recipient,
        string line1,
        string? line2,
        string city,
        string country,
        string postalCode,
        CancellationToken cancellationToken)
    {
        return await sender.SendAsync(
            new StartOrderCommand(
                user,
                customerId,
                recipient,
                line1,
                line2,
                city,
                country,
                postalCode),
            cancellationToken);
    }

    [Authorize]
    [Error<OrderNotFoundException>]
    [Error<ProductUnavailableException>]
    [Error<UnauthorizedAccessException>]
    public async ValueTask<Order> AddOrderLineAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID<Order>] Guid orderId,
        [ID<Product>] Guid productId,
        int quantity,
        CancellationToken cancellationToken)
    {
        return await sender.SendAsync(
            new AddOrderLineCommand(user, orderId, productId, quantity),
            cancellationToken);
    }

    [Authorize]
    [Error<OrderNotFoundException>]
    [Error<UnauthorizedAccessException>]
    public async ValueTask<Order> SubmitOrderAsync(
        ClaimsPrincipal user,
        ISender sender,
        [ID<Order>] Guid orderId,
        CancellationToken cancellationToken)
    {
        return await sender.SendAsync(new SubmitOrderCommand(user, orderId), cancellationToken);
    }
}
