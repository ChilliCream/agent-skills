namespace Reference.VerticalSlice.Domain;

public sealed class Book
{
    private Book()
    {
    }

    private Book(Guid id, Guid authorId, string title, DateTimeOffset createdAt)
    {
        Id = id;
        AuthorId = authorId;
        Title = NormalizeTitle(title);
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid AuthorId { get; private set; }

    public Author? Author { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public static Book Create(Guid authorId, string title)
    {
        if (authorId == Guid.Empty)
        {
            throw new ArgumentException("Author id is required.", nameof(authorId));
        }

        return new Book(Guid.CreateVersion7(), authorId, title, DateTimeOffset.UtcNow);
    }

    public void Rename(string title)
    {
        Title = NormalizeTitle(title);
    }

    private static string NormalizeTitle(string title)
    {
        var normalized = title.Trim();

        if (normalized.Length == 0)
        {
            throw new ArgumentException("Book title cannot be empty.", nameof(title));
        }

        if (normalized.Length > 300)
        {
            throw new ArgumentException("Book title cannot exceed 300 characters.", nameof(title));
        }

        return normalized;
    }
}
