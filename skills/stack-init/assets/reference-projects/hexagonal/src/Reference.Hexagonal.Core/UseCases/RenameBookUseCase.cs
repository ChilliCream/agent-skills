using Reference.Hexagonal.Core.Domain;
using Reference.Hexagonal.Core.Exceptions;
using Reference.Hexagonal.Core.Ports.In;
using Reference.Hexagonal.Core.Ports.Out;

namespace Reference.Hexagonal.Core.UseCases;

public sealed class RenameBookUseCase(IBookStore books) : IRenameBook
{
    public async ValueTask<Book> HandleAsync(
        RenameBookInput input,
        CancellationToken cancellationToken)
    {
        var book = await books.FindByIdAsync(input.BookId, cancellationToken)
            ?? throw new BookNotFoundException(input.BookId);

        book.Rename(input.Title);

        await books.SaveChangesAsync(cancellationToken);

        return book;
    }
}
