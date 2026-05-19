namespace Reference.Ddd.Application.Catalog.Products.Errors;

public sealed class ProductNotFoundException(Guid productId)
    : Exception($"Product '{productId}' was not found.")
{
    public Guid ProductId { get; } = productId;
}
