using Reference.Hexagonal.Adapters.Persistence.DataLoaders;
using Reference.Hexagonal.Core.Domain;
using Reference.Hexagonal.Core.Ports.Out;

namespace Reference.Hexagonal.Adapters.Persistence.Ports;

internal sealed class DataLoaderBookLookup(IBookBatchingContext books) : IBookLookup
{
    public async ValueTask<Book?> FindByIdAsync(
        Guid bookId,
        CancellationToken cancellationToken)
        => await books.BookById.LoadAsync(bookId, cancellationToken);

    public async ValueTask<IReadOnlyList<Book>> FindByAuthorIdAsync(
        Guid authorId,
        CancellationToken cancellationToken)
    {
        var result = await books.BooksByAuthorId.LoadAsync(authorId, cancellationToken);
        return result?.ToArray() ?? [];
    }
}
