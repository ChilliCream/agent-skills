using Reference.Ddd.Catalog.Products.Events;
using Reference.Ddd.SharedKernel;

namespace Reference.Ddd.Catalog.Products;

public sealed class Product : AggregateRoot
{
    private Product()
    {
        Sku = null!;
        Name = string.Empty;
        Price = null!;
    }

    private Product(Guid id, Sku sku, string name, string? description, Money price)
    {
        Id = id;
        Sku = sku;
        Name = name;
        Description = description;
        Price = price;
        Status = ProductStatus.Active;
    }

    public Guid Id { get; private set; }

    public Sku Sku { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public Money Price { get; private set; }

    public ProductStatus Status { get; private set; }

    public static Product Create(Sku sku, string name, string? description, Money price)
    {
        var product = new Product(
            Guid.CreateVersion7(),
            sku,
            NormalizeName(name),
            NormalizeDescription(description),
            price);

        product.Raise(new ProductCreatedEvent(product.Id, product.Sku, DateTimeOffset.UtcNow));
        return product;
    }

    public void Rename(string name, string? description)
    {
        EnsureCanChangeCatalogData();
        Name = NormalizeName(name);
        Description = NormalizeDescription(description);
    }

    public void ChangePrice(Money price)
    {
        EnsureCanChangeCatalogData();

        if (Price == price)
        {
            return;
        }

        var oldPrice = Price;
        Price = price;
        Raise(new ProductPriceChangedEvent(Id, oldPrice, price, DateTimeOffset.UtcNow));
    }

    public void Retire()
    {
        if (Status == ProductStatus.Retired)
        {
            return;
        }

        Status = ProductStatus.Retired;
        Raise(new ProductRetiredEvent(Id, DateTimeOffset.UtcNow));
    }

    private void EnsureCanChangeCatalogData()
    {
        if (Status == ProductStatus.Retired)
        {
            throw new InvalidOperationException("Retired products cannot be changed.");
        }
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        var normalized = name.Trim();
        if (normalized.Length > 160)
        {
            throw new ArgumentException("Product name cannot exceed 160 characters.", nameof(name));
        }

        return normalized;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var normalized = description.Trim();
        if (normalized.Length > 2000)
        {
            throw new ArgumentException("Product description cannot exceed 2000 characters.", nameof(description));
        }

        return normalized;
    }
}
