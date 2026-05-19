using Reference.CleanArchitecture.Domain.Authors;
using Reference.CleanArchitecture.Domain.Authors.Events;
using Reference.CleanArchitecture.Domain.Books;
using Reference.CleanArchitecture.Domain.Books.Events;
using Xunit;

namespace Reference.CleanArchitecture.Domain.Tests.Authors;

public sealed class AuthorTests
{
    [Fact]
    public void AddBook_ShouldCreateBookAndRaiseEvents()
    {
        var now = new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var author = Author.Register(new AuthorName("Octavia Butler"), now);

        var book = author.AddBook("Kindred", Isbn.Parse("9780807083697"), now);

        Assert.Equal(author.Id, book.AuthorId);
        Assert.Equal("Kindred", book.Title);
        Assert.Contains(author.Events, e => e is AuthorRegisteredEvent);
        Assert.Contains(author.Events, e => e is BookAddedToAuthorEvent);
        Assert.Contains(book.Events, e => e is BookCreatedEvent);
    }

    [Fact]
    public void AddBook_ShouldRejectDuplicateIsbnWithinAuthor()
    {
        var author = Author.Register(new AuthorName("Ursula K. Le Guin"), DateTimeOffset.UtcNow);
        author.AddBook("A Wizard of Earthsea", Isbn.Parse("9780547773742"), DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            author.AddBook("Duplicate", Isbn.Parse("9780547773742"), DateTimeOffset.UtcNow));
    }
}
