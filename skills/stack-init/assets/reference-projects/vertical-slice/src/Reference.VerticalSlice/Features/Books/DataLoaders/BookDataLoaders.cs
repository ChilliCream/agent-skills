using Reference.VerticalSlice.Domain;
using Reference.VerticalSlice.Shared.Persistence;

namespace Reference.VerticalSlice.Features.Books.DataLoaders;

[DataLoaderGroup("BookBatchingContext")]
public static class BookDataLoaders
{
    [DataLoader(Lookups = [nameof(GetBookByIdLookup)])]
    public static async Task<Dictionary<Guid, Book>> GetBookByIdAsync(
        IReadOnlyList<Guid> keys,
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        return await context.Books
            .AsNoTracking()
            .Where(book => keys.Contains(book.Id))
            .ToDictionaryAsync(book => book.Id, cancellationToken);
    }

    public static Guid GetBookByIdLookup(Book book) => book.Id;

    [DataLoader]
    public static async Task<ILookup<Guid, Book>> GetBooksByAuthorIdAsync(
        IReadOnlyList<Guid> keys,
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        var books = await context.Books
            .AsNoTracking()
            .Where(book => keys.Contains(book.AuthorId))
            .OrderBy(book => book.Title)
            .ThenBy(book => book.Id)
            .ToListAsync(cancellationToken);

        return books.ToLookup(book => book.AuthorId);
    }
}
