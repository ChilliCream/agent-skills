using GreenDonut;
using GreenDonut.Data;
using Microsoft.EntityFrameworkCore;
using Reference.Hexagonal.Core.Domain;

namespace Reference.Hexagonal.Adapters.Persistence.DataLoaders;

[DataLoaderGroup("BookBatchingContext")]
public static class BookDataLoaders
{
    [DataLoader(Lookups = [nameof(GetBookByIdLookup)])]
    public static async Task<Dictionary<Guid, Book>> GetBookByIdAsync(
        IReadOnlyList<Guid> keys,
        LibraryDbContext context,
        CancellationToken cancellationToken)
    {
        return await context.Books
            .AsNoTracking()
            .Where(x => keys.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
    }

    public static Guid GetBookByIdLookup(Book book) => book.Id;

    [DataLoader]
    public static async Task<ILookup<Guid, Book>> GetBooksByAuthorIdAsync(
        IReadOnlyList<Guid> keys,
        LibraryDbContext context,
        CancellationToken cancellationToken)
    {
        var books = await context.Books
            .AsNoTracking()
            .Where(x => keys.Contains(x.AuthorId))
            .OrderBy(x => x.Title)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return books.ToLookup(x => x.AuthorId);
    }
}
