using Mocha.Mediator;
using Reference.Hexagonal.Adapters.GraphQL.Authors.Queries;
using Reference.Hexagonal.Core.Domain;

namespace Reference.Hexagonal.Adapters.GraphQL.Authors.Operations;

[QueryType]
public sealed class AuthorQueries
{
    public ValueTask<Author?> GetAuthorByIdAsync(
        ISender sender,
        [ID<Author>] Guid id,
        CancellationToken cancellationToken)
        => sender.QueryAsync(new GetAuthorByIdQuery(id), cancellationToken);
}
