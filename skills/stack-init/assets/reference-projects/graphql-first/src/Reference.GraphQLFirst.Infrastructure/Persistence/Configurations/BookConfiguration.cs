using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reference.GraphQLFirst.Domain.Authors;
using Reference.GraphQLFirst.Domain.Books;

namespace Reference.GraphQLFirst.Infrastructure.Persistence.Configurations;

public sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("books");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.AuthorId).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Isbn).HasMaxLength(13);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.AuthorId);
        builder.HasIndex(x => x.Isbn).IsUnique().HasFilter("isbn IS NOT NULL");

        builder
            .HasOne<Author>()
            .WithMany()
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
