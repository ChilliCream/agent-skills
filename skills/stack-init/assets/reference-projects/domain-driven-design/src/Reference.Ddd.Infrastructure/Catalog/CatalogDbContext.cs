using Microsoft.EntityFrameworkCore;
using Reference.Ddd.Application.Catalog.Abstractions;
using Reference.Ddd.Catalog.Products;
using Reference.Ddd.Infrastructure.Catalog.Configurations;

namespace Reference.Ddd.Infrastructure.Catalog;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : DbContext(options), ICatalogDbContext
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
    }
}
