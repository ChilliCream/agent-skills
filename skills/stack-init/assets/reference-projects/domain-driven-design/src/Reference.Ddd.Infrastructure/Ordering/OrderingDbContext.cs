using Microsoft.EntityFrameworkCore;
using Reference.Ddd.Application.Ordering.Abstractions;
using Reference.Ddd.Infrastructure.Ordering.Configurations;
using Reference.Ddd.Ordering.Orders;

namespace Reference.Ddd.Infrastructure.Ordering;

public sealed class OrderingDbContext(DbContextOptions<OrderingDbContext> options)
    : DbContext(options), IOrderingDbContext
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
    }
}
