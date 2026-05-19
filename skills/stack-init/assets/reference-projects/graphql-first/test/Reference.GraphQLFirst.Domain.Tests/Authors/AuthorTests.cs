using Reference.GraphQLFirst.Domain.Authors;
using Reference.GraphQLFirst.Domain.Common;
using Xunit;

namespace Reference.GraphQLFirst.Domain.Tests.Authors;

public sealed class AuthorTests
{
    [Fact]
    public void Create_ShouldNormalizeName_WhenNameHasExtraWhitespace()
    {
        var author = Author.Create("  Octavia Butler  ", DateTimeOffset.UnixEpoch);

        Assert.Equal("Octavia Butler", author.Name);
    }

    [Fact]
    public void Create_ShouldThrowDomainValidationException_WhenNameIsBlank()
    {
        Assert.Throws<DomainValidationException>(() =>
            Author.Create(" ", DateTimeOffset.UnixEpoch));
    }
}
