using Reference.Hexagonal.Core.Domain;

namespace Reference.Hexagonal.Core.Ports.In;

public sealed record GetBookByIdInput(Guid BookId);

public interface IGetBookById
{
    ValueTask<Book?> HandleAsync(GetBookByIdInput input, CancellationToken cancellationToken);
}
