using Xunit;
using Reference.Hexagonal.Core.Domain;
using Reference.Hexagonal.Core.Exceptions;
using Reference.Hexagonal.Core.Ports.In;
using Reference.Hexagonal.Core.Ports.Out;
using Reference.Hexagonal.Core.UseCases;

namespace Reference.Hexagonal.Core.Tests;

public sealed class CreateAuthorUseCaseTests
{
    [Fact]
    public async Task HandleAsync_ShouldCreateAuthor_WhenNameIsUnique()
    {
        var authors = new InMemoryAuthorStore();
        var useCase = new CreateAuthorUseCase(authors);

        var author = await useCase.HandleAsync(
            new CreateAuthorInput("Ursula K. Le Guin", "Earthsea and Hainish Cycle author."),
            CancellationToken.None);

        Assert.Equal("Ursula K. Le Guin", author.Name);
        Assert.Equal(author, authors.Saved.Single());
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowDuplicateAuthorNameException_WhenNameExists()
    {
        var authors = new InMemoryAuthorStore();
        authors.Seed(Author.Create("Ursula K. Le Guin", null));
        var useCase = new CreateAuthorUseCase(authors);

        await Assert.ThrowsAsync<DuplicateAuthorNameException>(async () =>
            await useCase.HandleAsync(
                new CreateAuthorInput("Ursula K. Le Guin", null),
                CancellationToken.None));
    }

    private sealed class InMemoryAuthorStore : IAuthorStore
    {
        private readonly List<Author> _authors = [];

        public IReadOnlyList<Author> Saved => _authors;

        public void Seed(Author author) => _authors.Add(author);

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
}
