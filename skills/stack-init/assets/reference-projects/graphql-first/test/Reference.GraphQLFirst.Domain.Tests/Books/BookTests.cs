using Reference.GraphQLFirst.Domain.Books;
using Reference.GraphQLFirst.Domain.Common;
using Xunit;

namespace Reference.GraphQLFirst.Domain.Tests.Books;

public sealed class BookTests
{
    [Fact]
    public void Create_ShouldNormalizeTitleAndIsbn_WhenInputIsValid()
    {
        var book = Book.Create(
            Guid.NewGuid(),
            "  Kindred  ",
            1979,
            "978-0807083697",
            DateTimeOffset.UnixEpoch);

        Assert.Equal("Kindred", book.Title);
        Assert.Equal("9780807083697", book.Isbn);
    }

    [Fact]
    public void Create_ShouldThrowDomainValidationException_WhenAuthorIdIsEmpty()
    {
        Assert.Throws<DomainValidationException>(() =>
            Book.Create(Guid.Empty, "Kindred", 1979, null, DateTimeOffset.UnixEpoch));
    }
}
