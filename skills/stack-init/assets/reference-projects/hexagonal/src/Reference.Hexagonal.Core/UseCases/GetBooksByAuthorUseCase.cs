using Reference.Hexagonal.Core.Domain;
using Reference.Hexagonal.Core.Ports.In;
using Reference.Hexagonal.Core.Ports.Out;

namespace Reference.Hexagonal.Core.UseCases;

public sealed class GetBooksByAuthorUseCase(IBookLookup books) : IGetBooksByAuthor
{
    public ValueTask<IReadOnlyList<Book>> HandleAsync(
        GetBooksByAuthorInput input,
        CancellationToken cancellationToken)
        => books.FindByAuthorIdAsync(input.AuthorId, cancellationToken);
}
