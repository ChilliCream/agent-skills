using Reference.Ddd.SharedKernel;

namespace Reference.Ddd.Ordering.Orders;

public sealed class OrderLine
{
    private OrderLine()
    {
        ProductSku = string.Empty;
        ProductName = string.Empty;
        UnitPrice = null!;
    }

    private OrderLine(
        Guid id,
        Guid productId,
        string productSku,
        string productName,
        Money unitPrice,
        int quantity)
    {
        Id = id;
        ProductId = productId;
        ProductSku = productSku;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    public string ProductSku { get; private set; }

    public string ProductName { get; private set; }

    public Money UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    public Money Subtotal => UnitPrice.Multiply(Quantity);

    internal static OrderLine Create(
        Guid productId,
        string productSku,
        string productName,
        Money unitPrice,
        int quantity)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product id is required.", nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(productSku))
        {
            throw new ArgumentException("Product SKU is required.", nameof(productSku));
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException("Product name is required.", nameof(productName));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        return new OrderLine(
            Guid.CreateVersion7(),
            productId,
            productSku.Trim(),
            productName.Trim(),
            unitPrice,
            quantity);
    }

    internal void IncreaseQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        Quantity += quantity;
    }

    internal void ChangeQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        Quantity = quantity;
    }
}
