using System.Security.Claims;
using GreenDonut;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Mocha.Mediator;
using Reference.Ddd.Application.Catalog.Abstractions;
using Reference.Ddd.Application.Ordering.Abstractions;
using Reference.Ddd.Application.Ordering.Orders.Errors;
using Reference.Ddd.Application.Security;
using Reference.Ddd.Catalog.Products;
using Reference.Ddd.Ordering.Orders;

namespace Reference.Ddd.Application.Ordering.Orders.Commands;

public sealed record AddOrderLineCommand(
    ClaimsPrincipal User,
    Guid OrderId,
    Guid ProductId,
    int Quantity)
    : ICommand<Order>;

public sealed class AddOrderLineCommandHandler(
    IOrderingDbContext ordering,
    ICatalogDbContext catalog,
    IPromiseCache cache,
    IAuthorizationService authorization)
    : ICommandHandler<AddOrderLineCommand, Order>
{
    public async ValueTask<Order> HandleAsync(
        AddOrderLineCommand command,
        CancellationToken cancellationToken)
    {
        var (user, orderId, productId, quantity) = command;

        if (user.Identity is not { IsAuthenticated: true })
        {
            throw new UnauthorizedAccessException();
        }

        var order = await ordering.Orders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);

        if (order is null)
        {
            throw new OrderNotFoundException(orderId);
        }

        cache.Publish(order);

        var result = await authorization.AuthorizeAsync(user, order, ReferencePolicies.OrdersWrite);
        if (!result.Succeeded)
        {
            throw new OrderNotFoundException(orderId);
        }

        var product = await catalog.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == productId, cancellationToken);

        if (product is null || product.Status != ProductStatus.Active)
        {
            throw new ProductUnavailableException(productId);
        }

        order.AddLine(product.Id, product.Sku.Value, product.Name, product.Price, quantity);
        cache.Publish(order);

        await ordering.SaveChangesAsync(cancellationToken);
        return order;
    }
}
