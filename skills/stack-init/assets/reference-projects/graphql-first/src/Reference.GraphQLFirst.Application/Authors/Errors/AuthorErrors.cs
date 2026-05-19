namespace Reference.GraphQLFirst.Application.Authors.Errors;

public sealed class AuthorNotFoundException(Guid authorId)
    : Exception("Author was not found.")
{
    public Guid AuthorId { get; } = authorId;
}

public sealed class DuplicateAuthorNameException(string name)
    : Exception("An author with the same name already exists.")
{
    public string Name { get; } = name;
}
