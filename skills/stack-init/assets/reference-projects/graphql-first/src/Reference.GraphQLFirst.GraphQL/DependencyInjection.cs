using GreenDonut;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reference.GraphQLFirst.GraphQL.Authors.Operations;
using Reference.GraphQLFirst.GraphQL.Authors.Types;
using Reference.GraphQLFirst.GraphQL.Books.Operations;
using Reference.GraphQLFirst.GraphQL.Books.Types;

namespace Reference.GraphQLFirst.GraphQL;

public static class DependencyInjection
{
    public static IRequestExecutorBuilder AddReferenceGraphQLFirstGraphQL(
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
            .AddAuthorization()
            .AddGlobalObjectIdentification()
            .AddMutationConventions(applyToAllMutations: true);
    }
}
