namespace Reference.Hexagonal.Core.Exceptions;

public sealed class AuthorNotFoundException(Guid authorId)
    : Exception($"Author '{authorId}' was not found.")
{
    public Guid AuthorId { get; } = authorId;
}
