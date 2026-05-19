using Reference.Hexagonal.Core.Domain;

namespace Reference.Hexagonal.Core.Ports.In;

public sealed record GetAuthorByIdInput(Guid AuthorId);

public interface IGetAuthorById
{
    ValueTask<Author?> HandleAsync(GetAuthorByIdInput input, CancellationToken cancellationToken);
}
