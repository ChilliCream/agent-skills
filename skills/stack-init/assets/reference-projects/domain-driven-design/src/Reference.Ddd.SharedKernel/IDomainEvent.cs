namespace Reference.Ddd.SharedKernel;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
