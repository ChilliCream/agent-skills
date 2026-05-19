using GreenDonut;
using GreenDonut.Data;
using Microsoft.EntityFrameworkCore;
using Reference.GraphQLFirst.Application.Abstractions;
using Reference.GraphQLFirst.Domain.Authors;

namespace Reference.GraphQLFirst.Application.Authors.DataLoaders;

[DataLoaderGroup("AuthorBatchingContext")]
public static class AuthorDataLoaders
{
    [DataLoader(Lookups = [nameof(GetAuthorByIdLookup)])]
    public static async Task<Dictionary<Guid, Author>> GetAuthorByIdAsync(
        IReadOnlyList<Guid> keys,
        IReferenceDbContext context,
        CancellationToken cancellationToken)
    {
        return await context.Authors
            .AsNoTracking()
            .Where(x => keys.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
    }

    public static Guid GetAuthorByIdLookup(Author author) => author.Id;
}
