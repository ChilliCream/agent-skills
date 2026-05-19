using Microsoft.EntityFrameworkCore;
using Reference.Hexagonal.Core.Domain;
using Reference.Hexagonal.Core.Ports.Out;

namespace Reference.Hexagonal.Adapters.Persistence.Ports;

internal sealed class EfAuthorStore(LibraryDbContext context) : IAuthorStore
{
    public async ValueTask<Author?> FindByIdAsync(
        Guid authorId,
        CancellationToken cancellationToken)
        => await context.Authors.FirstOrDefaultAsync(x => x.Id == authorId, cancellationToken);

    public async ValueTask<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken)
        => await context.Authors.AnyAsync(x => x.Name == name.Trim(), cancellationToken);

    public ValueTask AddAsync(Author author, CancellationToken cancellationToken)
    {
        context.Authors.Add(author);
        return ValueTask.CompletedTask;
    }

    public async ValueTask SaveChangesAsync(CancellationToken cancellationToken)
        => await context.SaveChangesAsync(cancellationToken);
}
