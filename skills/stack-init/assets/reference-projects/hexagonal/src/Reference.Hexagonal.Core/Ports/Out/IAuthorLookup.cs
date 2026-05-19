using Reference.Hexagonal.Core.Domain;

namespace Reference.Hexagonal.Core.Ports.Out;

public interface IAuthorLookup
{
    ValueTask<Author?> FindByIdAsync(Guid authorId, CancellationToken cancellationToken);
}
