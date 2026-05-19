using Reference.Hexagonal.Core.Domain;
using Reference.Hexagonal.Core.Exceptions;
using Reference.Hexagonal.Core.Ports.In;
using Reference.Hexagonal.Core.Ports.Out;

namespace Reference.Hexagonal.Core.UseCases;

public sealed class CreateAuthorUseCase(IAuthorStore authors) : ICreateAuthor
{
    public async ValueTask<Author> HandleAsync(
        CreateAuthorInput input,
        CancellationToken cancellationToken)
    {
        var author = Author.Create(input.Name, input.Biography);

        if (await authors.ExistsByNameAsync(author.Name, cancellationToken))
        {
            throw new DuplicateAuthorNameException(author.Name);
        }

        await authors.AddAsync(author, cancellationToken);
        await authors.SaveChangesAsync(cancellationToken);

        return author;
    }
}
