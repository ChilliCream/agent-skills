using GreenDonut;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Mediator;

namespace Reference.Ddd.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddReferenceDddApplication(this IServiceCollection services)
    {
        services
            .AddMediator()
            .AddApplication();

        services.AddScoped<IPromiseCache>(sp => sp.GetRequiredService<PromiseCacheOwner>().Cache);

        return services;
    }
}
