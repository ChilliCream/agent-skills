using Reference.Ddd.SharedKernel;

namespace Reference.Ddd.Catalog.Products.Events;

public sealed record ProductPriceChangedEvent(
    Guid ProductId,
    Money OldPrice,
    Money NewPrice,
    DateTimeOffset OccurredAt)
    : IDomainEvent;
