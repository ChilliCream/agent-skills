using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Reference.Ddd.GraphQL;

public static class DependencyInjection
{
    public static IRequestExecutorBuilder AddReferenceDddGraphQL(this IServiceCollection services)
    {
        return services
            .AddGraphQLServer()
            .AddAuthorization()
            .AddMutationConventions()
            .AddGlobalObjectIdentification()
            .AddTypes()
            .AddApplicationTypes();
    }
}
