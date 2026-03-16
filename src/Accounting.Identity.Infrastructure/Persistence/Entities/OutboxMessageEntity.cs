namespace Accounting.Identity.Infrastructure.Persistence.Entities;

/// <summary>
/// Persistence entity for outbox messages.
/// The Outbox pattern ensures reliable event publishing by storing events
/// in the same transaction as the aggregate, then publishing them asynchronously.
/// </summary>
public class OutboxMessageEntity
{
    /// <summary>
    /// Unique identifier for the outbox message.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The type of the domain event (fully qualified type name).
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Serialized JSON payload of the event.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the message has been published to the message broker.
    /// </summary>
    public bool Published { get; set; }

    /// <summary>
    /// Timestamp when the message was published (null if not yet published).
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Timestamp when the message was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Number of times publishing this message has been attempted (for retry tracking).
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// Timestamp of the last failed publication attempt.
    /// </summary>
    public DateTime? LastAttemptAt { get; set; }

    /// <summary>
    /// Error message from the last failed publication attempt.
    /// </summary>
    public string? LastError { get; set; }
}
