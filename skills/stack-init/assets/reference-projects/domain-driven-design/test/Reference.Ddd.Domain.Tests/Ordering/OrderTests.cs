using Reference.Ddd.Ordering.Orders;
using Reference.Ddd.Ordering.Orders.Events;
using Reference.Ddd.SharedKernel;
using Xunit;

namespace Reference.Ddd.Domain.Tests.Ordering;

public sealed class OrderTests
{
    [Fact]
    public void AddLine_ShouldStoreProductSnapshotAndIdOnly()
    {
        var order = Order.Start(Guid.NewGuid(), Address());
        var productId = Guid.NewGuid();

        order.AddLine(productId, "MUG", "GraphQL Mug", new Money(12m, "USD"), 2);

        var line = Assert.Single(order.Lines);
        Assert.Equal(productId, line.ProductId);
        Assert.Equal("MUG", line.ProductSku);
        Assert.Equal("GraphQL Mug", line.ProductName);
        Assert.Equal(new Money(24m, "USD"), line.Subtotal);
        Assert.Contains(order.DomainEvents, x => x is OrderLineAddedEvent);
    }

    [Fact]
    public void Submit_ShouldRequireAtLeastOneLine()
    {
        var order = Order.Start(Guid.NewGuid(), Address());

        Assert.Throws<InvalidOperationException>(() => order.Submit());
    }

    [Fact]
    public void Submit_ShouldFreezeDraftOrder()
    {
        var order = Order.Start(Guid.NewGuid(), Address());
        order.AddLine(Guid.NewGuid(), "MUG", "GraphQL Mug", new Money(12m, "USD"), 1);

        order.Submit();

        Assert.Equal(OrderStatus.Submitted, order.Status);
        Assert.IsType<OrderSubmittedEvent>(order.DomainEvents.Last());
        Assert.Throws<InvalidOperationException>(() =>
            order.AddLine(Guid.NewGuid(), "TEE", "GraphQL Tee", new Money(20m, "USD"), 1));
    }

    private static ShippingAddress Address()
        => new("Ada Lovelace", "1 Compiler Way", null, "London", "gb", "N1 1AA");
}
