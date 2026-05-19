using Reference.Hexagonal.Core.Domain;
using Reference.Hexagonal.Core.Exceptions;
using Reference.Hexagonal.Core.Ports.In;
using Reference.Hexagonal.Core.Ports.Out;

namespace Reference.Hexagonal.Core.UseCases;

public sealed class RegisterBookUseCase(
    IAuthorStore authors,
    IBookStore books) : IRegisterBook
{
    public async ValueTask<Book> HandleAsync(
        RegisterBookInput input,
        CancellationToken cancellationToken)
    {
        var isbn = Isbn.Parse(input.Isbn);

        if (await authors.FindByIdAsync(input.AuthorId, cancellationToken) is null)
        {
            throw new AuthorNotFoundException(input.AuthorId);
        }

        if (await books.IsIsbnAssignedAsync(isbn, cancellationToken))
        {
            throw new DuplicateIsbnException(isbn);
        }

        var book = Book.Register(
            input.AuthorId,
            isbn,
            input.Title,
            input.Synopsis,
            input.PublishedOn);

        await books.AddAsync(book, cancellationToken);
        await books.SaveChangesAsync(cancellationToken);

        return book;
    }
}
