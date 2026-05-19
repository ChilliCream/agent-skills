using Reference.Ddd.Ordering.Orders.Events;
using Reference.Ddd.SharedKernel;

namespace Reference.Ddd.Ordering.Orders;

public sealed class Order : AggregateRoot
{
    private readonly List<OrderLine> _lines = [];

    private Order()
    {
        ShippingAddress = null!;
    }

    private Order(Guid id, Guid customerId, ShippingAddress shippingAddress, DateTimeOffset createdAt)
    {
        Id = id;
        CustomerId = customerId;
        ShippingAddress = shippingAddress;
        CreatedAt = createdAt;
        Status = OrderStatus.Draft;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public OrderStatus Status { get; private set; }

    public ShippingAddress ShippingAddress { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? SubmittedAt { get; private set; }

    public IReadOnlyList<OrderLine> Lines => _lines;

    public Money Total => _lines.Count == 0
        ? Money.Zero("USD")
        : _lines.Select(x => x.Subtotal).Aggregate((left, right) => left.Add(right));

    public static Order Start(Guid customerId, ShippingAddress shippingAddress)
    {
        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("Customer id is required.", nameof(customerId));
        }

        var order = new Order(Guid.CreateVersion7(), customerId, shippingAddress, DateTimeOffset.UtcNow);
        order.Raise(new OrderStartedEvent(order.Id, customerId, order.CreatedAt));
        return order;
    }

    public void AddLine(
        Guid productId,
        string productSku,
        string productName,
        Money unitPrice,
        int quantity)
    {
        EnsureDraft();
        EnsureSameCurrency(unitPrice);

        var existingLine = _lines.FirstOrDefault(x => x.ProductId == productId);
        if (existingLine is not null)
        {
            existingLine.IncreaseQuantity(quantity);
            return;
        }

        var line = OrderLine.Create(productId, productSku, productName, unitPrice, quantity);
        _lines.Add(line);
        Raise(new OrderLineAddedEvent(Id, line.Id, productId, DateTimeOffset.UtcNow));
    }

    public void ChangeLineQuantity(Guid orderLineId, int quantity)
    {
        EnsureDraft();

        var line = _lines.FirstOrDefault(x => x.Id == orderLineId);
        if (line is null)
        {
            throw new InvalidOperationException("Order line was not found.");
        }

        line.ChangeQuantity(quantity);
    }

    public void Submit()
    {
        EnsureDraft();

        if (_lines.Count == 0)
        {
            throw new InvalidOperationException("An order must contain at least one line before it can be submitted.");
        }

        Status = OrderStatus.Submitted;
        SubmittedAt = DateTimeOffset.UtcNow;
        Raise(new OrderSubmittedEvent(Id, Total, SubmittedAt.Value));
    }

    private void EnsureDraft()
    {
        if (Status != OrderStatus.Draft)
        {
            throw new InvalidOperationException("Only draft orders can be changed.");
        }
    }

    private void EnsureSameCurrency(Money unitPrice)
    {
        if (_lines.Count > 0 && !StringComparer.Ordinal.Equals(_lines[0].UnitPrice.Currency, unitPrice.Currency))
        {
            throw new InvalidOperationException("All lines in an order must use the same currency.");
        }
    }
}
