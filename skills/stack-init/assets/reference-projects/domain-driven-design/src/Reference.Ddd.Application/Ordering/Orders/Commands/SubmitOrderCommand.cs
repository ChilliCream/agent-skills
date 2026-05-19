using System.Security.Claims;
using GreenDonut;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Mocha.Mediator;
using Reference.Ddd.Application.Ordering.Abstractions;
using Reference.Ddd.Application.Ordering.Orders.Errors;
using Reference.Ddd.Application.Security;
using Reference.Ddd.Ordering.Orders;

namespace Reference.Ddd.Application.Ordering.Orders.Commands;

public sealed record SubmitOrderCommand(ClaimsPrincipal User, Guid OrderId) : ICommand<Order>;

public sealed class SubmitOrderCommandHandler(
    IOrderingDbContext context,
    IPromiseCache cache,
    IAuthorizationService authorization)
    : ICommandHandler<SubmitOrderCommand, Order>
{
    public async ValueTask<Order> HandleAsync(
        SubmitOrderCommand command,
        CancellationToken cancellationToken)
    {
        var (user, orderId) = command;

        if (user.Identity is not { IsAuthenticated: true })
        {
            throw new UnauthorizedAccessException();
        }

        var order = await context.Orders
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

        order.Submit();
        cache.Publish(order);

        await context.SaveChangesAsync(cancellationToken);
        return order;
    }
}
