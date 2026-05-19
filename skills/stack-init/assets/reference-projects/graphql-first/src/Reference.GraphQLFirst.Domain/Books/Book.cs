using Reference.GraphQLFirst.Domain.Common;

namespace Reference.GraphQLFirst.Domain.Books;

public sealed class Book
{
    private Book()
    {
        Title = string.Empty;
    }

    private Book(
        Guid id,
        Guid authorId,
        string title,
        int? publicationYear,
        string? isbn,
        DateTimeOffset createdAt)
    {
        Id = id;
        AuthorId = authorId;
        Title = title;
        PublicationYear = publicationYear;
        Isbn = isbn;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid AuthorId { get; private set; }

    public string Title { get; private set; }

    public int? PublicationYear { get; private set; }

    public string? Isbn { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Book Create(
        Guid authorId,
        string title,
        int? publicationYear,
        string? isbn,
        DateTimeOffset createdAt)
    {
        if (authorId == Guid.Empty)
        {
            throw new DomainValidationException("A book must belong to an author.");
        }

        return new Book(
            Guid.CreateVersion7(),
            authorId,
            NormalizeTitle(title),
            ValidatePublicationYear(publicationYear),
            NormalizeIsbn(isbn),
            createdAt);
    }

    public void Rename(string title)
        => Title = NormalizeTitle(title);

    public static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainValidationException("Book title is required.");
        }

        var normalized = title.Trim();

        if (normalized.Length > 300)
        {
            throw new DomainValidationException("Book title cannot exceed 300 characters.");
        }

        return normalized;
    }

    public static string? NormalizeIsbn(string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return null;
        }

        var normalized = isbn.Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Trim()
            .ToUpperInvariant();

        if (normalized.Length is not (10 or 13))
        {
            throw new DomainValidationException("ISBN must be 10 or 13 characters.");
        }

        return normalized;
    }

    private static int? ValidatePublicationYear(int? publicationYear)
    {
        if (publicationYear is null)
        {
            return null;
        }

        var latestAllowedYear = DateTimeOffset.UtcNow.Year + 1;

        if (publicationYear < 1450 || publicationYear > latestAllowedYear)
        {
            throw new DomainValidationException("Publication year is outside the supported range.");
        }

        return publicationYear;
    }
}
