namespace Reference.CleanArchitecture.Application.Authors.Errors;

public sealed class AuthorNotFoundException(Guid authorId)
    : Exception($"Author '{authorId}' was not found.")
{
    public Guid AuthorId { get; } = authorId;
}
