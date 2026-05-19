using Reference.Hexagonal.Core.Domain;
using Reference.Hexagonal.Core.Ports.In;
using Reference.Hexagonal.Core.Ports.Out;

namespace Reference.Hexagonal.Core.UseCases;

public sealed class GetAuthorByIdUseCase(IAuthorLookup authors) : IGetAuthorById
{
    public ValueTask<Author?> HandleAsync(
        GetAuthorByIdInput input,
        CancellationToken cancellationToken)
        => authors.FindByIdAsync(input.AuthorId, cancellationToken);
}
