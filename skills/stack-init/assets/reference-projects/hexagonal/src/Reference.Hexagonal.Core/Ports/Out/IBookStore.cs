using Reference.Hexagonal.Core.Domain;

namespace Reference.Hexagonal.Core.Ports.Out;

public interface IBookStore
{
    ValueTask<Book?> FindByIdAsync(Guid bookId, CancellationToken cancellationToken);

    ValueTask<bool> IsIsbnAssignedAsync(Isbn isbn, CancellationToken cancellationToken);

    ValueTask AddAsync(Book book, CancellationToken cancellationToken);

    ValueTask SaveChangesAsync(CancellationToken cancellationToken);
}
