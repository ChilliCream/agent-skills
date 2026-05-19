using Reference.Hexagonal.Core.Domain;
using Reference.Hexagonal.Core.Ports.In;
using Reference.Hexagonal.Core.Ports.Out;

namespace Reference.Hexagonal.Core.UseCases;

public sealed class GetBookByIdUseCase(IBookLookup books) : IGetBookById
{
    public ValueTask<Book?> HandleAsync(
        GetBookByIdInput input,
        CancellationToken cancellationToken)
        => books.FindByIdAsync(input.BookId, cancellationToken);
}
