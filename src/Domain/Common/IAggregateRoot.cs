namespace Domain.Common;

public interface IAggregateRoot
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}