using Reference.Hexagonal.Core.Domain;

namespace Reference.Hexagonal.Core.Ports.In;

public sealed record RegisterBookInput(
    Guid AuthorId,
    string Isbn,
    string Title,
    string? Synopsis,
    DateOnly? PublishedOn);

public interface IRegisterBook
{
    ValueTask<Book> HandleAsync(RegisterBookInput input, CancellationToken cancellationToken);
}
