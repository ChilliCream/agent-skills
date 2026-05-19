using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reference.Ddd.Application.Catalog.Abstractions;
using Reference.Ddd.Application.Ordering.Abstractions;
using Reference.Ddd.Infrastructure.Catalog;
using Reference.Ddd.Infrastructure.Ordering;

namespace Reference.Ddd.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddReferenceDddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ReferenceDdd")
            ?? "Data Source=reference-ddd.db";

        services.AddDbContext<CatalogDbContext>(options => options.UseSqlite(connectionString));
        services.AddDbContext<OrderingDbContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<ICatalogDbContext>(sp => sp.GetRequiredService<CatalogDbContext>());
        services.AddScoped<IOrderingDbContext>(sp => sp.GetRequiredService<OrderingDbContext>());

        return services;
    }
}
