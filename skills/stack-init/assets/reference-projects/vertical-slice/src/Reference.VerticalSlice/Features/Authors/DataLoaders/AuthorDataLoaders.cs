using Reference.VerticalSlice.Domain;
using Reference.VerticalSlice.Shared.Persistence;

namespace Reference.VerticalSlice.Features.Authors.DataLoaders;

[DataLoaderGroup("AuthorBatchingContext")]
public static class AuthorDataLoaders
{
    [DataLoader(Lookups = [nameof(GetAuthorByIdLookup)])]
    public static async Task<Dictionary<Guid, Author>> GetAuthorByIdAsync(
        IReadOnlyList<Guid> keys,
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        return await context.Authors
            .AsNoTracking()
            .Where(author => keys.Contains(author.Id))
            .ToDictionaryAsync(author => author.Id, cancellationToken);
    }

    public static Guid GetAuthorByIdLookup(Author author) => author.Id;
}
