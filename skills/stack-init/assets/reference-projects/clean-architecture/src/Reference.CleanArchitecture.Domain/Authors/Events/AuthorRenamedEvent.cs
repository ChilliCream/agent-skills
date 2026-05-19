using Reference.CleanArchitecture.Domain.Common;

namespace Reference.CleanArchitecture.Domain.Authors.Events;

public sealed record AuthorRenamedEvent(Guid AuthorId, DateTimeOffset OccurredAt) : IDomainEvent;
