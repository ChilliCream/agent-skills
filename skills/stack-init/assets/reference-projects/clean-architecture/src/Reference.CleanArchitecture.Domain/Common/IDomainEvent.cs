namespace Reference.CleanArchitecture.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
