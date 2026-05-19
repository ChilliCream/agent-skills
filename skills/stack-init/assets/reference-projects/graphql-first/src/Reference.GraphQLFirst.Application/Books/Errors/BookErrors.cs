namespace Reference.GraphQLFirst.Application.Books.Errors;

public sealed class BookNotFoundException(Guid bookId)
    : Exception("Book was not found.")
{
    public Guid BookId { get; } = bookId;
}

public sealed class DuplicateBookIsbnException(string isbn)
    : Exception("A book with the same ISBN already exists.")
{
    public string Isbn { get; } = isbn;
}
