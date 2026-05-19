using Mocha.Mediator;
using Reference.Hexagonal.Core.Domain;
using Reference.Hexagonal.Core.Ports.In;

namespace Reference.Hexagonal.Adapters.GraphQL.Books.Queries;

public sealed record GetBooksByAuthorIdQuery(Guid AuthorId) : IQuery<IReadOnlyList<Book>>;

public sealed class GetBooksByAuthorIdQueryHandler(IGetBooksByAuthor getBooksByAuthor)
    : IQueryHandler<GetBooksByAuthorIdQuery, IReadOnlyList<Book>>
{
    public ValueTask<IReadOnlyList<Book>> HandleAsync(
        GetBooksByAuthorIdQuery query,
        CancellationToken cancellationToken)
        => getBooksByAuthor.HandleAsync(
            new GetBooksByAuthorInput(query.AuthorId),
            cancellationToken);
}
