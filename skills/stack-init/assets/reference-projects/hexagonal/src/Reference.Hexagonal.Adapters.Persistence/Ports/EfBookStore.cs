using Microsoft.EntityFrameworkCore;
using Reference.Hexagonal.Core.Domain;
using Reference.Hexagonal.Core.Ports.Out;

namespace Reference.Hexagonal.Adapters.Persistence.Ports;

internal sealed class EfBookStore(LibraryDbContext context) : IBookStore
{
    public async ValueTask<Book?> FindByIdAsync(
        Guid bookId,
        CancellationToken cancellationToken)
        => await context.Books.FirstOrDefaultAsync(x => x.Id == bookId, cancellationToken);

    public async ValueTask<bool> IsIsbnAssignedAsync(
        Isbn isbn,
        CancellationToken cancellationToken)
        => await context.Books.AnyAsync(x => x.Isbn == isbn, cancellationToken);

    public ValueTask AddAsync(Book book, CancellationToken cancellationToken)
    {
        context.Books.Add(book);
        return ValueTask.CompletedTask;
    }

    public async ValueTask SaveChangesAsync(CancellationToken cancellationToken)
        => await context.SaveChangesAsync(cancellationToken);
}
