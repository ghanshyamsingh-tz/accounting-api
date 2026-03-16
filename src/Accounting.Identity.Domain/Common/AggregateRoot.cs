namespace Accounting.Identity.Domain.Common;

/// <summary>
/// Base class for aggregate roots in the Domain layer.
/// Aggregate roots are the entry points for all changes to their aggregate.
/// They maintain invariants and raise domain events.
/// </summary>
/// <typeparam name="TId">The type of the aggregate's identifier.</typeparam>
public abstract class AggregateRoot<TId> where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Gets the unique identifier of the aggregate root.
    /// </summary>
    public TId Id { get; protected init; } = default!;

    /// <summary>
    /// Gets the domain events that have been raised by the aggregate.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Raises a domain event.
    /// The event will be published after the aggregate is persisted.
    /// </summary>
    /// <param name="domainEvent">The domain event to raise.</param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all domain events.
    /// This should be called after events have been published.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Override this to implement custom equality based on the aggregate's identity.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
        {
            return false;
        }

        var other = (AggregateRoot<TId>)obj;
        return Id.Equals(other.Id);
    }

    /// <summary>
    /// Override this to implement custom hash code based on the aggregate's identity.
    /// </summary>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    /// <summary>
    /// Equality operator based on aggregate identity.
    /// </summary>
    public static bool operator ==(AggregateRoot<TId>? left, AggregateRoot<TId>? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator based on aggregate identity.
    /// </summary>
    public static bool operator !=(AggregateRoot<TId>? left, AggregateRoot<TId>? right)
    {
        return !(left == right);
    }
}
