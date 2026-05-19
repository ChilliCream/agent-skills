using Reference.CleanArchitecture.Domain.Authors.Events;
using Reference.CleanArchitecture.Domain.Books;
using Reference.CleanArchitecture.Domain.Common;

namespace Reference.CleanArchitecture.Domain.Authors;

public sealed class Author : Entity
{
    private readonly List<Book> _books = [];

    private Author()
    {
    }

    private Author(Guid id, AuthorName name, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        CreatedAt = createdAt;
    }

    public AuthorName Name { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public IReadOnlyCollection<Book> Books => _books;

    public static Author Register(AuthorName name, DateTimeOffset now)
    {
        var author = new Author(Guid.CreateVersion7(), name, now);
        author.Raise(new AuthorRegisteredEvent(author.Id, now));
        return author;
    }

    public void Rename(AuthorName name, DateTimeOffset now)
    {
        if (Name == name)
        {
            return;
        }

        Name = name;
        UpdatedAt = now;
        Raise(new AuthorRenamedEvent(Id, now));
    }

    public Book AddBook(string title, Isbn isbn, DateTimeOffset now)
    {
        if (_books.Any(x => x.Isbn == isbn))
        {
            throw new InvalidOperationException("The author already has a book with this ISBN.");
        }

        var book = Book.Create(Id, title, isbn, now);
        _books.Add(book);
        Raise(new BookAddedToAuthorEvent(Id, book.Id, now));
        return book;
    }
}
