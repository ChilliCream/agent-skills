using Reference.Ddd.SharedKernel;

namespace Reference.Ddd.Catalog.Products.Events;

public sealed record ProductRetiredEvent(Guid ProductId, DateTimeOffset OccurredAt) : IDomainEvent;
