using GreenDonut;
using Microsoft.EntityFrameworkCore;
using Reference.CleanArchitecture.Application.Common;
using Reference.CleanArchitecture.Domain.Books;

namespace Reference.CleanArchitecture.Application.Books.DataLoaders;

[DataLoaderGroup("BookBatchingContext")]
public static class BookDataLoaders
{
    [DataLoader(Lookups = [nameof(GetBookByIdLookup)])]
    public static async Task<Dictionary<Guid, Book>> GetBookByIdAsync(
        IReadOnlyList<Guid> keys,
        IAppDbContext context,
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
        IAppDbContext context,
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
