using Reference.Hexagonal.Adapters.Persistence.DataLoaders;
using Reference.Hexagonal.Core.Domain;
using Reference.Hexagonal.Core.Ports.Out;

namespace Reference.Hexagonal.Adapters.Persistence.Ports;

internal sealed class DataLoaderAuthorLookup(IAuthorBatchingContext authors) : IAuthorLookup
{
    public async ValueTask<Author?> FindByIdAsync(
        Guid authorId,
        CancellationToken cancellationToken)
        => await authors.AuthorById.LoadAsync(authorId, cancellationToken);
}
