using Reference.Ddd.SharedKernel;

namespace Reference.Ddd.Ordering.Orders.Events;

public sealed record OrderStartedEvent(Guid OrderId, Guid CustomerId, DateTimeOffset OccurredAt)
    : IDomainEvent;
