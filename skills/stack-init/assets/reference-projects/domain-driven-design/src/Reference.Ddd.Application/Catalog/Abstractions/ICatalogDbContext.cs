using Microsoft.EntityFrameworkCore;
using Reference.Ddd.Catalog.Products;

namespace Reference.Ddd.Application.Catalog.Abstractions;

public interface ICatalogDbContext
{
    DbSet<Product> Products { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
