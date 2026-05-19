using GreenDonut;
using GreenDonut.Data;
using Microsoft.EntityFrameworkCore;
using Reference.Ddd.Application.Ordering.Abstractions;
using Reference.Ddd.Ordering.Orders;

namespace Reference.Ddd.Application.Ordering.Orders.DataLoaders;

[DataLoaderGroup("OrderBatchingContext")]
public static class OrderDataLoaders
{
    [DataLoader(Lookups = [nameof(GetOrderByIdLookup)])]
    public static async Task<Dictionary<Guid, Order>> GetOrderByIdAsync(
        IReadOnlyList<Guid> keys,
        IOrderingDbContext context,
        CancellationToken cancellationToken)
    {
        return await context.Orders
            .AsNoTracking()
            .Include(x => x.Lines)
            .Where(x => keys.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
    }

    public static Guid GetOrderByIdLookup(Order order) => order.Id;

    [DataLoader]
    public static async Task<Dictionary<Guid, Page<Order>>> PageOrdersByCustomerIdAsync(
        IReadOnlyList<Guid> keys,
        PagingArguments arguments,
        IOrderingDbContext context,
        CancellationToken cancellationToken)
    {
        return await context.Orders
            .AsNoTracking()
            .Where(x => keys.Contains(x.CustomerId))
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToBatchPageAsync(x => x.CustomerId, arguments, cancellationToken);
    }
}
