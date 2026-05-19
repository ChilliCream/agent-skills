using Reference.CleanArchitecture.Domain.Common;

namespace Reference.CleanArchitecture.Domain.Books.Events;

public sealed record BookCreatedEvent(
    Guid BookId,
    Guid AuthorId,
    DateTimeOffset OccurredAt) : IDomainEvent;
