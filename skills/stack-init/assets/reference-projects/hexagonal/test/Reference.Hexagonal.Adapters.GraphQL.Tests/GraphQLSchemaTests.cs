using Xunit;
namespace Reference.Hexagonal.Adapters.GraphQL.Tests;

public sealed class GraphQLSchemaTests
{
    [Fact(Skip = "Skeleton: compose Host services, execute a schema request, and snapshot the Query/Mutation/Node surface.")]
    public Task Schema_ShouldExposeCatalogQueriesMutationsAndRelayNodes()
        => Task.CompletedTask;

    [Fact(Skip = "Skeleton: execute createBook and assert the mutation payload plus DataLoader-backed author field selection.")]
    public Task CreateBookMutation_ShouldDispatchThroughMochaAndResolveAuthorThroughDataLoader()
        => Task.CompletedTask;
}
