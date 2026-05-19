using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reference.VerticalSlice.Domain;

namespace Reference.VerticalSlice.Shared.Persistence.Configurations;

public sealed class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> builder)
    {
        builder.ToTable("Authors");
        builder.HasKey(author => author.Id);

        builder.Property(author => author.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(author => author.CreatedAt)
            .IsRequired();

        builder.HasMany(author => author.Books)
            .WithOne(book => book.Author)
            .HasForeignKey(book => book.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(author => author.Name);
    }
}
