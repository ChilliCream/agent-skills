using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reference.GraphQLFirst.Application.Abstractions;
using Reference.GraphQLFirst.Infrastructure.Persistence;

namespace Reference.GraphQLFirst.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddReferencePersistence(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<ReferenceDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IReferenceDbContext>(provider => provider.GetRequiredService<ReferenceDbContext>());

        return services;
    }
}
