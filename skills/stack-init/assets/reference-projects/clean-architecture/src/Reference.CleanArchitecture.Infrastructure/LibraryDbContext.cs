using Microsoft.EntityFrameworkCore;
using Reference.CleanArchitecture.Application.Common;
using Reference.CleanArchitecture.Domain.Authors;
using Reference.CleanArchitecture.Domain.Books;

namespace Reference.CleanArchitecture.Infrastructure;

public sealed class LibraryDbContext(DbContextOptions<LibraryDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<Author> Authors => Set<Author>();

    public DbSet<Book> Books => Set<Book>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryDbContext).Assembly);
    }
}
