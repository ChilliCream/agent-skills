using System.Security.Claims;
using GreenDonut.Data;
using Microsoft.AspNetCore.Authorization;
using Mocha.Mediator;
using Reference.Ddd.Application.Ordering.Orders.DataLoaders;
using Reference.Ddd.Application.Security;
using Reference.Ddd.Ordering.Orders;

namespace Reference.Ddd.Application.Ordering.Orders.Queries;

public sealed record PageOrdersByCustomerIdQuery(
    ClaimsPrincipal User,
    Guid CustomerId,
    PagingArguments Paging)
    : IQuery<Page<Order>?>;

public sealed class PageOrdersByCustomerIdQueryHandler(
    IOrderBatchingContext orders,
    IAuthorizationService authorization)
    : IQueryHandler<PageOrdersByCustomerIdQuery, Page<Order>?>
{
    public async ValueTask<Page<Order>?> HandleAsync(
        PageOrdersByCustomerIdQuery query,
        CancellationToken cancellationToken)
    {
        var (user, customerId, paging) = query;

        if (user.Identity is not { IsAuthenticated: true })
        {
            return null;
        }

        var result = await authorization.AuthorizeAsync(user, customerId, ReferencePolicies.OrdersRead);
        if (!result.Succeeded)
        {
            return null;
        }

        return await orders.PageOrdersByCustomerId.With(paging).LoadAsync(customerId, cancellationToken);
    }
}
