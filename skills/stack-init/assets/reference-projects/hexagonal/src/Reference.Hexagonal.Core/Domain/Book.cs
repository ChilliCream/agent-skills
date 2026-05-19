namespace Reference.Hexagonal.Core.Domain;

public sealed class Book
{
    private Book()
    {
    }

    private Book(
        Guid id,
        Guid authorId,
        Isbn isbn,
        string title,
        string? synopsis,
        DateOnly? publishedOn)
    {
        Id = id;
        AuthorId = authorId;
        Isbn = isbn;
        Title = title;
        Synopsis = synopsis;
        PublishedOn = publishedOn;
    }

    public Guid Id { get; private set; }

    public Guid AuthorId { get; private set; }

    public Isbn Isbn { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    public string? Synopsis { get; private set; }

    public DateOnly? PublishedOn { get; private set; }

    public static Book Register(
        Guid authorId,
        Isbn isbn,
        string title,
        string? synopsis,
        DateOnly? publishedOn)
    {
        if (authorId == Guid.Empty)
        {
            throw new ArgumentException("Author id is required.", nameof(authorId));
        }

        return new Book(
            Guid.CreateVersion7(),
            authorId,
            isbn,
            NormalizeTitle(title),
            NormalizeSynopsis(synopsis),
            publishedOn);
    }

    public void Rename(string title)
    {
        Title = NormalizeTitle(title);
    }

    public void ChangeSynopsis(string? synopsis)
    {
        Synopsis = NormalizeSynopsis(synopsis);
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Book title is required.", nameof(title));
        }

        var normalized = title.Trim();
        if (normalized.Length > 300)
        {
            throw new ArgumentException("Book title cannot exceed 300 characters.", nameof(title));
        }

        return normalized;
    }

    private static string? NormalizeSynopsis(string? synopsis)
    {
        if (string.IsNullOrWhiteSpace(synopsis))
        {
            return null;
        }

        var normalized = synopsis.Trim();
        if (normalized.Length > 4000)
        {
            throw new ArgumentException("Synopsis cannot exceed 4000 characters.", nameof(synopsis));
        }

        return normalized;
    }
}
