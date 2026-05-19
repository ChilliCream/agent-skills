using Reference.CleanArchitecture.Domain.Books.Events;
using Reference.CleanArchitecture.Domain.Common;

namespace Reference.CleanArchitecture.Domain.Books;

public sealed class Book : Entity
{
    private Book()
    {
        Title = string.Empty;
    }

    private Book(Guid id, Guid authorId, string title, Isbn isbn, DateTimeOffset createdAt)
    {
        Id = id;
        AuthorId = authorId;
        Title = NormalizeTitle(title);
        Isbn = isbn;
        CreatedAt = createdAt;
    }

    public Guid AuthorId { get; private set; }

    public string Title { get; private set; }

    public Isbn Isbn { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public bool IsPublished { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    internal static Book Create(Guid authorId, string title, Isbn isbn, DateTimeOffset now)
    {
        var book = new Book(Guid.CreateVersion7(), authorId, title, isbn, now);
        book.Raise(new BookCreatedEvent(book.Id, authorId, now));
        return book;
    }

    public void Rename(string title, DateTimeOffset now)
    {
        var normalized = NormalizeTitle(title);
        if (Title == normalized)
        {
            return;
        }

        Title = normalized;
        UpdatedAt = now;
    }

    public void Publish(DateTimeOffset now)
    {
        if (IsPublished)
        {
            return;
        }

        IsPublished = true;
        PublishedAt = now;
        Raise(new BookPublishedEvent(Id, now));
    }

    private static string NormalizeTitle(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var normalized = title.Trim();
        if (normalized.Length > 300)
        {
            throw new ArgumentOutOfRangeException(nameof(title), "Book title cannot exceed 300 characters.");
        }

        return normalized;
    }
}
