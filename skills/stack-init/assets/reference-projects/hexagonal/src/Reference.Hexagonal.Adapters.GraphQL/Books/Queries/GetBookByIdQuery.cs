using Mocha.Mediator;
using Reference.Hexagonal.Core.Domain;
using Reference.Hexagonal.Core.Ports.In;

namespace Reference.Hexagonal.Adapters.GraphQL.Books.Queries;

public sealed record GetBookByIdQuery(Guid BookId) : IQuery<Book?>;

public sealed class GetBookByIdQueryHandler(IGetBookById getBookById)
    : IQueryHandler<GetBookByIdQuery, Book?>
{
    public ValueTask<Book?> HandleAsync(
        GetBookByIdQuery query,
        CancellationToken cancellationToken)
        => getBookById.HandleAsync(
            new GetBookByIdInput(query.BookId),
            cancellationToken);
}
