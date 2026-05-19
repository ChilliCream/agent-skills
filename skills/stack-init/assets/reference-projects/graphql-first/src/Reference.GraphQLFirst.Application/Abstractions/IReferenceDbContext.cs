using Microsoft.EntityFrameworkCore;
using Reference.GraphQLFirst.Domain.Authors;
using Reference.GraphQLFirst.Domain.Books;

namespace Reference.GraphQLFirst.Application.Abstractions;

public interface IReferenceDbContext
{
    DbSet<Author> Authors { get; }

    DbSet<Book> Books { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
