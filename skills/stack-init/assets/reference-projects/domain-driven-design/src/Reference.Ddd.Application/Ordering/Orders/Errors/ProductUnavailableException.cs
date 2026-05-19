namespace Reference.Ddd.Application.Ordering.Orders.Errors;

public sealed class ProductUnavailableException(Guid productId)
    : Exception($"Product '{productId}' is not available for ordering.")
{
    public Guid ProductId { get; } = productId;
}
