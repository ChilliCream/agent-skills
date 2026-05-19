using Reference.Hexagonal.Core.Domain;

namespace Reference.Hexagonal.Core.Ports.Out;

public interface IBookLookup
{
    ValueTask<Book?> FindByIdAsync(Guid bookId, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<Book>> FindByAuthorIdAsync(
        Guid authorId,
        CancellationToken cancellationToken);
}
