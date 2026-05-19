using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reference.CleanArchitecture.Domain.Authors;

namespace Reference.CleanArchitecture.Infrastructure.Authors.Configurations;

internal sealed class AuthorEntityConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Ignore(x => x.Events);

        builder.Property(x => x.Name)
            .HasConversion(
                name => name.Value,
                value => new AuthorName(value))
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);

        builder.HasMany(x => x.Books)
            .WithOne()
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Books)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
