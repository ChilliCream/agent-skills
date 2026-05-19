using Reference.Ddd.Catalog.Products;
using Reference.Ddd.Catalog.Products.Events;
using Reference.Ddd.SharedKernel;
using Xunit;

namespace Reference.Ddd.Domain.Tests.Catalog;

public sealed class ProductTests
{
    [Fact]
    public void Create_ShouldNormalizeSkuAndRaiseEvent()
    {
        var product = Product.Create(new Sku(" sku-1 "), " GraphQL Mug ", null, new Money(12.345m, "usd"));

        Assert.Equal("SKU-1", product.Sku.Value);
        Assert.Equal("GraphQL Mug", product.Name);
        Assert.Equal(new Money(12.35m, "USD"), product.Price);
        Assert.IsType<ProductCreatedEvent>(Assert.Single(product.DomainEvents));
    }

    [Fact]
    public void ChangePrice_ShouldRaisePriceChangedEvent()
    {
        var product = Product.Create(new Sku("MUG"), "Mug", null, new Money(10m, "USD"));
        product.DequeueDomainEvents();

        product.ChangePrice(new Money(11m, "USD"));

        var domainEvent = Assert.IsType<ProductPriceChangedEvent>(Assert.Single(product.DomainEvents));
        Assert.Equal(new Money(10m, "USD"), domainEvent.OldPrice);
        Assert.Equal(new Money(11m, "USD"), domainEvent.NewPrice);
    }

    [Fact]
    public void Rename_ShouldRejectRetiredProduct()
    {
        var product = Product.Create(new Sku("MUG"), "Mug", null, new Money(10m, "USD"));
        product.Retire();

        Assert.Throws<InvalidOperationException>(() => product.Rename("New mug", null));
    }
}
