using Accounting.Identity.Domain.Common;

namespace Accounting.Identity.Application.Interfaces;

/// <summary>
/// Interface for processing outbox messages.
/// The Outbox pattern ensures reliable event publishing by storing events
/// in the same database transaction as the aggregate, then publishing them asynchronously.
/// </summary>
public interface IOutboxProcessor
{
    /// <summary>
    /// Processes unpublished messages from the outbox.
    /// Should be called periodically by a background worker.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of messages processed.</returns>
    Task<int> ProcessUnpublishedMessagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a domain event to the outbox for later publishing.
    /// </summary>
    /// <param name="domainEvent">The domain event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddToOutboxAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
