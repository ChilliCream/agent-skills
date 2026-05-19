namespace Reference.VerticalSlice.Domain;

public sealed class Author
{
    private Author()
    {
    }

    private Author(Guid id, string name, DateTimeOffset createdAt)
    {
        Id = id;
        Name = NormalizeName(name);
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public List<Book> Books { get; private set; } = [];

    public static Author Create(string name)
    {
        return new Author(Guid.CreateVersion7(), name, DateTimeOffset.UtcNow);
    }

    public void Rename(string name)
    {
        Name = NormalizeName(name);
    }

    private static string NormalizeName(string name)
    {
        var normalized = name.Trim();

        if (normalized.Length == 0)
        {
            throw new ArgumentException("Author name cannot be empty.", nameof(name));
        }

        if (normalized.Length > 200)
        {
            throw new ArgumentException("Author name cannot exceed 200 characters.", nameof(name));
        }

        return normalized;
    }
}
