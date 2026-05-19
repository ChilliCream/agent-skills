namespace Reference.CleanArchitecture.Application.Books.Errors;

public sealed class BookNotFoundException(Guid bookId)
    : Exception($"Book '{bookId}' was not found.")
{
    public Guid BookId { get; } = bookId;
}
