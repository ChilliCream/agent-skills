namespace Reference.Hexagonal.Core.Exceptions;

public sealed class BookNotFoundException(Guid bookId)
    : Exception($"Book '{bookId}' was not found.")
{
    public Guid BookId { get; } = bookId;
}
