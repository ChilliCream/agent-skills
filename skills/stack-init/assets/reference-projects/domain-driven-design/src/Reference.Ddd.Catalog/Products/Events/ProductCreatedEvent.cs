using Reference.Ddd.SharedKernel;

namespace Reference.Ddd.Catalog.Products.Events;

public sealed record ProductCreatedEvent(Guid ProductId, Sku Sku, DateTimeOffset OccurredAt)
    : IDomainEvent;
