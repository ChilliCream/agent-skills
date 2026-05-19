using GreenDonut;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reference.Hexagonal.Adapters.Persistence.Ports;
using Reference.Hexagonal.Core.Ports.Out;

namespace Reference.Hexagonal.Adapters.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddHexagonalPersistence(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        services.AddDbContext<LibraryDbContext>(configureDbContext);

        services.AddHexagonalPersistenceDataLoaders();
        services.AddScoped<IPromiseCache>(sp => sp.GetRequiredService<PromiseCacheOwner>().Cache);

        services.AddScoped<IAuthorStore, EfAuthorStore>();
        services.AddScoped<IBookStore, EfBookStore>();
        services.AddScoped<IAuthorLookup, DataLoaderAuthorLookup>();
        services.AddScoped<IBookLookup, DataLoaderBookLookup>();

        return services;
    }
}
