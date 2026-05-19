using Xunit;

namespace Reference.GraphQLFirst.GraphQL.Tests;

public sealed class SchemaSnapshotTests
{
    public static string SnapshotPath => Path.Combine(
        "__snapshots__",
        "schema.snap");

    [Fact(Skip = "Reference placeholder: build IRequestExecutor from Host DI, print schema, and compare to SnapshotPath.")]
    public Task Schema_ShouldMatchSnapshot()
        => Task.CompletedTask;
}
