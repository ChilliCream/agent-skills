using GreenDonut;
using Microsoft.EntityFrameworkCore;
using Reference.CleanArchitecture.Application.Common;
using Reference.CleanArchitecture.Domain.Authors;

namespace Reference.CleanArchitecture.Application.Authors.DataLoaders;

[DataLoaderGroup("AuthorBatchingContext")]
public static class AuthorDataLoaders
{
    [DataLoader(Lookups = [nameof(GetAuthorByIdLookup)])]
    public static async Task<Dictionary<Guid, Author>> GetAuthorByIdAsync(
        IReadOnlyList<Guid> keys,
        IAppDbContext context,
        CancellationToken cancellationToken)
    {
        return await context.Authors
            .AsNoTracking()
            .Where(x => keys.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
    }

    public static Guid GetAuthorByIdLookup(Author author) => author.Id;
}
