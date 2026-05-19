using Mocha.Mediator;
using Reference.Hexagonal.Core.Domain;
using Reference.Hexagonal.Core.Ports.In;

namespace Reference.Hexagonal.Adapters.GraphQL.Authors.Queries;

public sealed record GetAuthorByIdQuery(Guid AuthorId) : IQuery<Author?>;

public sealed class GetAuthorByIdQueryHandler(IGetAuthorById getAuthorById)
    : IQueryHandler<GetAuthorByIdQuery, Author?>
{
    public ValueTask<Author?> HandleAsync(
        GetAuthorByIdQuery query,
        CancellationToken cancellationToken)
        => getAuthorById.HandleAsync(
            new GetAuthorByIdInput(query.AuthorId),
            cancellationToken);
}
