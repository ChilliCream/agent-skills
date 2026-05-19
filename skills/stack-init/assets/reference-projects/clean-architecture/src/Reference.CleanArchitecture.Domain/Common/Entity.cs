namespace Reference.CleanArchitecture.Domain.Common;

public abstract class Entity
{
    private readonly List<IDomainEvent> _events = [];

    public Guid Id { get; protected set; }

    public IReadOnlyList<IDomainEvent> Events => _events;

    protected void Raise(IDomainEvent @event)
        => _events.Add(@event);

    public void ClearEvents()
        => _events.Clear();
}
