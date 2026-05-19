using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reference.Hexagonal.Core.Domain;

namespace Reference.Hexagonal.Adapters.Persistence.Configurations;

internal sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("books");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.AuthorId).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Synopsis).HasMaxLength(4000);
        builder.Property(x => x.PublishedOn);
        builder.Property(x => x.Isbn)
            .HasConversion(isbn => isbn.Value, value => Isbn.Parse(value))
            .HasMaxLength(13)
            .IsRequired();

        builder.HasIndex(x => x.Isbn).IsUnique();
        builder.HasIndex(x => x.AuthorId);

        builder.HasOne<Author>()
            .WithMany()
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
