using Reference.CleanArchitecture.Domain.Common;

namespace Reference.CleanArchitecture.Domain.Books.Events;

public sealed record BookPublishedEvent(Guid BookId, DateTimeOffset OccurredAt) : IDomainEvent;
