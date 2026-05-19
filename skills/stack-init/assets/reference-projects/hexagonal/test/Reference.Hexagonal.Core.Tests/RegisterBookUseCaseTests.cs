using Xunit;
using Reference.Hexagonal.Core.Domain;
using Reference.Hexagonal.Core.Exceptions;
using Reference.Hexagonal.Core.Ports.In;
using Reference.Hexagonal.Core.Ports.Out;
using Reference.Hexagonal.Core.UseCases;

namespace Reference.Hexagonal.Core.Tests;

public sealed class RegisterBookUseCaseTests
{
    [Fact]
    public async Task HandleAsync_ShouldRegisterBook_WhenAuthorExistsAndIsbnIsUnique()
    {
        var author = Author.Create("Octavia E. Butler", null);
        var authors = new InMemoryAuthorStore(author);
        var books = new InMemoryBookStore();
        var useCase = new RegisterBookUseCase(authors, books);

        var book = await useCase.HandleAsync(
            new RegisterBookInput(
                author.Id,
                "9780446675505",
                "Parable of the Sower",
                "A realistic reference aggregate.",
                new DateOnly(1993, 10, 1)),
            CancellationToken.None);

        Assert.Equal(author.Id, book.AuthorId);
        Assert.Equal("9780446675505", book.Isbn.Value);
        Assert.Equal(book, books.Saved.Single());
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowAuthorNotFoundException_WhenAuthorDoesNotExist()
    {
        var useCase = new RegisterBookUseCase(
            new InMemoryAuthorStore(),
            new InMemoryBookStore());

        await Assert.ThrowsAsync<AuthorNotFoundException>(async () =>
            await useCase.HandleAsync(
                new RegisterBookInput(Guid.CreateVersion7(), "9780446675505", "Title", null, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowDuplicateIsbnException_WhenIsbnExists()
    {
        var author = Author.Create("Octavia E. Butler", null);
        var authors = new InMemoryAuthorStore(author);
        var books = new InMemoryBookStore();
        books.Seed(Book.Register(author.Id, Isbn.Parse("9780446675505"), "Existing", null, null));
        var useCase = new RegisterBookUseCase(authors, books);

        await Assert.ThrowsAsync<DuplicateIsbnException>(async () =>
            await useCase.HandleAsync(
                new RegisterBookInput(author.Id, "9780446675505", "Duplicate", null, null),
                CancellationToken.None));
    }

    private sealed class InMemoryAuthorStore(params Author[] authors) : IAuthorStore
    {
        private readonly List<Author> _authors = [.. authors];

        public ValueTask<Author?> FindByIdAsync(Guid authorId, CancellationToken cancellationToken)
            => ValueTask.FromResult(_authors.FirstOrDefault(x => x.Id == authorId));

        public ValueTask<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken)
            => ValueTask.FromResult(_authors.Any(x => x.Name == name));

        public ValueTask AddAsync(Author author, CancellationToken cancellationToken)
        {
            _authors.Add(author);
            return ValueTask.CompletedTask;
        }

        public ValueTask SaveChangesAsync(CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    private sealed class InMemoryBookStore : IBookStore
    {
        private readonly List<Book> _books = [];

        public IReadOnlyList<Book> Saved => _books;

        public void Seed(Book book) => _books.Add(book);

        public ValueTask<Book?> FindByIdAsync(Guid bookId, CancellationToken cancellationToken)
            => ValueTask.FromResult(_books.FirstOrDefault(x => x.Id == bookId));

        public ValueTask<bool> IsIsbnAssignedAsync(Isbn isbn, CancellationToken cancellationToken)
            => ValueTask.FromResult(_books.Any(x => x.Isbn == isbn));

        public ValueTask AddAsync(Book book, CancellationToken cancellationToken)
        {
            _books.Add(book);
            return ValueTask.CompletedTask;
        }

        public ValueTask SaveChangesAsync(CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }
}
