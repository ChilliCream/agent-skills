using Reference.Ddd.SharedKernel;

namespace Reference.Ddd.Ordering.Orders.Events;

public sealed record OrderLineAddedEvent(
    Guid OrderId,
    Guid OrderLineId,
    Guid ProductId,
    DateTimeOffset OccurredAt)
    : IDomainEvent;
