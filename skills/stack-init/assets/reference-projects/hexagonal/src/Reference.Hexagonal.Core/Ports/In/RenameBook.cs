using Reference.Hexagonal.Core.Domain;

namespace Reference.Hexagonal.Core.Ports.In;

public sealed record RenameBookInput(Guid BookId, string Title);

public interface IRenameBook
{
    ValueTask<Book> HandleAsync(RenameBookInput input, CancellationToken cancellationToken);
}
