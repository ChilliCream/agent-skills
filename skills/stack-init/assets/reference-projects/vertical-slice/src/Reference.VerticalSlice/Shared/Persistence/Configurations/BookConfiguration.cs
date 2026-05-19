using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reference.VerticalSlice.Domain;

namespace Reference.VerticalSlice.Shared.Persistence.Configurations;

public sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");
        builder.HasKey(book => book.Id);

        builder.Property(book => book.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(book => book.CreatedAt)
            .IsRequired();

        builder.HasIndex(book => new { book.AuthorId, book.Title });
    }
}
