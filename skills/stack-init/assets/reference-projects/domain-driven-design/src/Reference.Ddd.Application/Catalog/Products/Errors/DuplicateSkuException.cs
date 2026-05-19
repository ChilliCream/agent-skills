namespace Reference.Ddd.Application.Catalog.Products.Errors;

public sealed class DuplicateSkuException(string sku)
    : Exception($"Product SKU '{sku}' is already in use.")
{
    public string Sku { get; } = sku;
}
