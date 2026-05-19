using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reference.CleanArchitecture.Domain.Books;

namespace Reference.CleanArchitecture.Infrastructure.Books.Configurations;

internal sealed class BookEntityConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Ignore(x => x.Events);

        builder.Property(x => x.AuthorId).IsRequired();
        builder.Property(x => x.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Isbn)
            .HasConversion(
                isbn => isbn.Value,
                value => Isbn.Parse(value))
            .HasMaxLength(13)
            .IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.IsPublished).IsRequired();
        builder.Property(x => x.PublishedAt);

        builder.HasIndex(x => new { x.AuthorId, x.Isbn }).IsUnique();
    }
}
