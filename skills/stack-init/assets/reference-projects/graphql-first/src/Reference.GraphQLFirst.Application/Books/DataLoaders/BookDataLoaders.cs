using GreenDonut;
using GreenDonut.Data;
using Microsoft.EntityFrameworkCore;
using Reference.GraphQLFirst.Application.Abstractions;
using Reference.GraphQLFirst.Domain.Books;

namespace Reference.GraphQLFirst.Application.Books.DataLoaders;

[DataLoaderGroup("BookBatchingContext")]
public static class BookDataLoaders
{
    [DataLoader(Lookups = [nameof(GetBookByIdLookup)])]
    public static async Task<Dictionary<Guid, Book>> GetBookByIdAsync(
        IReadOnlyList<Guid> keys,
        IReferenceDbContext context,
        CancellationToken cancellationToken)
    {
        return await context.Books
            .AsNoTracking()
            .Where(x => keys.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
    }

    public static Guid GetBookByIdLookup(Book book) => book.Id;

    [DataLoader]
    public static async Task<Dictionary<Guid, Page<Book>>> PageBooksByAuthorIdAsync(
        IReadOnlyList<Guid> keys,
        PagingArguments arguments,
        IReferenceDbContext context,
        CancellationToken cancellationToken)
    {
        return await context.Books
            .AsNoTracking()
            .Where(x => keys.Contains(x.AuthorId))
            .OrderBy(x => x.Title)
            .ThenBy(x => x.Id)
            .ToBatchPageAsync(x => x.AuthorId, arguments, cancellationToken);
    }
}
