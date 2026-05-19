using Reference.Ddd.SharedKernel;

namespace Reference.Ddd.Ordering.Orders.Events;

public sealed record OrderSubmittedEvent(Guid OrderId, Money Total, DateTimeOffset OccurredAt)
    : IDomainEvent;
