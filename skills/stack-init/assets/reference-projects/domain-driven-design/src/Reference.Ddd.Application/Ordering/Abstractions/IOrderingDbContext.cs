using Microsoft.EntityFrameworkCore;
using Reference.Ddd.Ordering.Orders;

namespace Reference.Ddd.Application.Ordering.Abstractions;

public interface IOrderingDbContext
{
    DbSet<Order> Orders { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
