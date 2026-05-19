using Reference.GraphQLFirst.Domain.Common;

namespace Reference.GraphQLFirst.Domain.Authors;

public sealed class Author
{
    private Author()
    {
        Name = string.Empty;
    }

    private Author(Guid id, string name, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Author Create(string name, DateTimeOffset createdAt)
        => new(Guid.CreateVersion7(), NormalizeName(name), createdAt);

    public void Rename(string name)
        => Name = NormalizeName(name);

    public static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Author name is required.");
        }

        var normalized = name.Trim();

        if (normalized.Length > 200)
        {
            throw new DomainValidationException("Author name cannot exceed 200 characters.");
        }

        return normalized;
    }
}
