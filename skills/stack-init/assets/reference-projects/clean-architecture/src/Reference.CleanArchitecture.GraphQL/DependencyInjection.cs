using GreenDonut;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using Reference.CleanArchitecture.GraphQL.Authors.Operations;
using Reference.CleanArchitecture.GraphQL.Authors.Types;
using Reference.CleanArchitecture.GraphQL.Books.Operations;
using Reference.CleanArchitecture.GraphQL.Books.Types;

namespace Reference.CleanArchitecture.GraphQL;

public static class DependencyInjection
{
    public static IRequestExecutorBuilder AddReferenceCleanArchitectureGraphQL(
        this IServiceCollection services)
    {
        services.AddScoped(sp => sp.GetRequiredService<PromiseCacheOwner>().Cache);

        return services
            .AddGraphQLServer()
            .AddTypes(
                typeof(AuthorQueries),
                typeof(AuthorMutations),
                typeof(AuthorType),
                typeof(BookQueries),
                typeof(BookMutations),
                typeof(BookType))
            .AddMutationConventions()
            .AddGlobalObjectIdentification()
            .AddAuthorization();
    }
}
