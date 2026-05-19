using Reference.Hexagonal.Core.Domain;

namespace Reference.Hexagonal.Core.Ports.Out;

public interface IAuthorStore
{
    ValueTask<Author?> FindByIdAsync(Guid authorId, CancellationToken cancellationToken);

    ValueTask<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);

    ValueTask AddAsync(Author author, CancellationToken cancellationToken);

    ValueTask SaveChangesAsync(CancellationToken cancellationToken);
}
