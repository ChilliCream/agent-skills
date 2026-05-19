using Reference.Hexagonal.Core.Domain;

namespace Reference.Hexagonal.Core.Ports.In;

public sealed record GetBooksByAuthorInput(Guid AuthorId);

public interface IGetBooksByAuthor
{
    ValueTask<IReadOnlyList<Book>> HandleAsync(
        GetBooksByAuthorInput input,
        CancellationToken cancellationToken);
}
