using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Mocha.Mediator;
using Reference.Ddd.Application.Ordering.Orders.DataLoaders;
using Reference.Ddd.Application.Security;
using Reference.Ddd.Ordering.Orders;

namespace Reference.Ddd.Application.Ordering.Orders.Queries;

public sealed record GetOrderByIdQuery(ClaimsPrincipal User, Guid Id) : IQuery<Order?>;

public sealed class GetOrderByIdQueryHandler(
    IOrderBatchingContext orders,
    IAuthorizationService authorization)
    : IQueryHandler<GetOrderByIdQuery, Order?>
{
    public async ValueTask<Order?> HandleAsync(
        GetOrderByIdQuery query,
        CancellationToken cancellationToken)
    {
        var (user, id) = query;

        if (user.Identity is not { IsAuthenticated: true })
        {
            return null;
        }

        var order = await orders.OrderById.LoadAsync(id, cancellationToken);
        if (order is null)
        {
            return null;
        }

        var result = await authorization.AuthorizeAsync(user, order, ReferencePolicies.OrdersRead);
        return result.Succeeded ? order : null;
    }
}
