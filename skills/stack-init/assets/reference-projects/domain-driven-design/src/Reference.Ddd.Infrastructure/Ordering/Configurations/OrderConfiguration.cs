using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reference.Ddd.Ordering.Orders;

namespace Reference.Ddd.Infrastructure.Ordering.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(x => x.Id);

        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.Total);

        builder.Property(x => x.CustomerId)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.OwnsOne(x => x.ShippingAddress, address =>
        {
            address.Property(x => x.Recipient).HasMaxLength(120).IsRequired();
            address.Property(x => x.Line1).HasMaxLength(200).IsRequired();
            address.Property(x => x.Line2).HasMaxLength(200);
            address.Property(x => x.City).HasMaxLength(120).IsRequired();
            address.Property(x => x.Country).HasMaxLength(2).IsRequired();
            address.Property(x => x.PostalCode).HasMaxLength(32).IsRequired();
        });
        builder.Navigation(x => x.ShippingAddress).IsRequired();

        builder.OwnsMany(x => x.Lines, line =>
        {
            line.ToTable("OrderLines");
            line.WithOwner().HasForeignKey("OrderId");
            line.HasKey(x => x.Id);

            line.Property(x => x.ProductId).IsRequired();
            line.Property(x => x.ProductSku).HasMaxLength(32).IsRequired();
            line.Property(x => x.ProductName).HasMaxLength(160).IsRequired();
            line.Property(x => x.Quantity).IsRequired();
            line.Ignore(x => x.Subtotal);

            line.OwnsOne(x => x.UnitPrice, money =>
            {
                money.Property(x => x.Amount)
                    .HasColumnName("UnitPriceAmount")
                    .HasPrecision(18, 2)
                    .IsRequired();

                money.Property(x => x.Currency)
                    .HasColumnName("UnitPriceCurrency")
                    .HasMaxLength(3)
                    .IsRequired();
            });
            line.Navigation(x => x.UnitPrice).IsRequired();
        });

        builder.Navigation(x => x.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
