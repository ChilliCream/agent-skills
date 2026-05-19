using Reference.CleanArchitecture.Domain.Common;

namespace Reference.CleanArchitecture.Domain.Authors.Events;

public sealed record BookAddedToAuthorEvent(
    Guid AuthorId,
    Guid BookId,
    DateTimeOffset OccurredAt) : IDomainEvent;
