using System.Security.Claims;
using GreenDonut;
using Microsoft.AspNetCore.Authorization;
using Mocha.Mediator;
using Reference.Ddd.Application.Ordering.Abstractions;
using Reference.Ddd.Application.Security;
using Reference.Ddd.Ordering.Orders;

namespace Reference.Ddd.Application.Ordering.Orders.Commands;

public sealed record StartOrderCommand(
    ClaimsPrincipal User,
    Guid CustomerId,
    string Recipient,
    string Line1,
    string? Line2,
    string City,
    string Country,
    string PostalCode)
    : ICommand<Order>;

public sealed class StartOrderCommandHandler(
    IOrderingDbContext context,
    IPromiseCache cache,
    IAuthorizationService authorization)
    : ICommandHandler<StartOrderCommand, Order>
{
    public async ValueTask<Order> HandleAsync(
        StartOrderCommand command,
        CancellationToken cancellationToken)
    {
        var (user, customerId, recipient, line1, line2, city, country, postalCode) = command;

        if (user.Identity is not { IsAuthenticated: true })
        {
            throw new UnauthorizedAccessException();
        }

        var result = await authorization.AuthorizeAsync(user, customerId, ReferencePolicies.OrdersWrite);
        if (!result.Succeeded)
        {
            throw new UnauthorizedAccessException();
        }

        var address = new ShippingAddress(recipient, line1, line2, city, country, postalCode);
        var order = Order.Start(customerId, address);

        context.Orders.Add(order);
        cache.Publish(order);

        await context.SaveChangesAsync(cancellationToken);
        return order;
    }
}
