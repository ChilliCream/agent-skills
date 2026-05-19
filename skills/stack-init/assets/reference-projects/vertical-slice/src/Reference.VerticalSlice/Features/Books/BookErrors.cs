using Reference.VerticalSlice.Domain;

namespace Reference.VerticalSlice.Features.Books;

public sealed class BookNotFoundException(Guid bookId)
    : Exception($"Book '{bookId}' was not found.")
{
    [ID(nameof(Book))]
    public Guid BookId { get; } = bookId;
}
