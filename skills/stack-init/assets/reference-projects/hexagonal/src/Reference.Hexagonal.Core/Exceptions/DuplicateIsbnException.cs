using Reference.Hexagonal.Core.Domain;

namespace Reference.Hexagonal.Core.Exceptions;

public sealed class DuplicateIsbnException(Isbn isbn)
    : Exception($"A book with ISBN '{isbn}' already exists.")
{
    public Isbn Isbn { get; } = isbn;
}
