using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reference.CleanArchitecture.Application.Common;

namespace Reference.CleanArchitecture.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddReferenceCleanArchitectureInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Library")
            ?? "Data Source=reference-clean-architecture.db";

        return services.AddReferenceCleanArchitectureInfrastructure(
            options => options.UseSqlite(connectionString));
    }

    public static IServiceCollection AddReferenceCleanArchitectureInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configure)
    {
        services.AddDbContext<LibraryDbContext>(configure);
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<LibraryDbContext>());
        return services;
    }
}
