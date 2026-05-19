using Microsoft.EntityFrameworkCore;
using Reference.CleanArchitecture.Domain.Authors;
using Reference.CleanArchitecture.Domain.Books;

namespace Reference.CleanArchitecture.Application.Common;

public interface IAppDbContext
{
    DbSet<Author> Authors { get; }

    DbSet<Book> Books { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
