using Microsoft.EntityFrameworkCore;
using Reference.GraphQLFirst.Application.Abstractions;
using Reference.GraphQLFirst.Domain.Authors;
using Reference.GraphQLFirst.Domain.Books;

namespace Reference.GraphQLFirst.Infrastructure.Persistence;

public sealed class ReferenceDbContext(DbContextOptions<ReferenceDbContext> options)
    : DbContext(options), IReferenceDbContext
{
    public DbSet<Author> Authors => Set<Author>();

    public DbSet<Book> Books => Set<Book>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReferenceDbContext).Assembly);
}
