using Accounting.Identity.Application.Interfaces;
using Accounting.Identity.Domain.Common;
using Accounting.Identity.Infrastructure.Persistence;
using Accounting.Identity.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Accounting.Identity.Infrastructure.Outbox;

/// <summary>
/// Processes outbox messages for reliable event publishing.
/// Implements the Outbox pattern to ensure events are published exactly once.
/// </summary>
public class OutboxProcessor : IOutboxProcessor
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly IEventPublisher _eventPublisher;
    private const int BatchSize = 20;
    private const int MaxRetries = 3;

    public OutboxProcessor(
        IdentityDbContext context,
        ILogger<OutboxProcessor> logger,
        IEventPublisher eventPublisher)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
    }

    /// <inheritdoc />
    public async Task<int> ProcessUnpublishedMessagesAsync(CancellationToken cancellationToken = default)
    {
        var messages = await _context.OutboxMessages
            .Where(m => !m.Published && m.AttemptCount < MaxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (!messages.Any())
        {
            return 0;
        }

        _logger.LogInformation(
            "Processing {Count} outbox messages",
            messages.Count);

        var processedCount = 0;

        foreach (var message in messages)
        {
            try
            {
                await PublishMessageAsync(message, cancellationToken);
                
                message.Published = true;
                message.PublishedAt = DateTime.UtcNow;
                processedCount++;

                _logger.LogDebug(
                    "Successfully published outbox message {MessageId} of type {EventType}",
                    message.Id,
                    message.EventType);
            }
            catch (Exception ex)
            {
                message.AttemptCount++;
                message.LastAttemptAt = DateTime.UtcNow;
                message.LastError = ex.Message;

                _logger.LogError(
                    ex,
                    "Failed to publish outbox message {MessageId} (attempt {AttemptCount}/{MaxRetries})",
                    message.Id,
                    message.AttemptCount,
                    MaxRetries);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Processed {ProcessedCount}/{TotalCount} outbox messages successfully",
            processedCount,
            messages.Count);

        return processedCount;
    }

    /// <inheritdoc />
    public async Task AddToOutboxAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var outboxMessage = new OutboxMessageEntity
        {
            Id = Guid.NewGuid(),
            EventType = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().FullName ?? "Unknown",
            Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            Published = false,
            CreatedAt = DateTime.UtcNow,
            AttemptCount = 0
        };

        await _context.OutboxMessages.AddAsync(outboxMessage, cancellationToken);

        _logger.LogDebug(
            "Added domain event {EventType} to outbox with ID {OutboxMessageId}",
            outboxMessage.EventType,
            outboxMessage.Id);
    }

    private async Task PublishMessageAsync(OutboxMessageEntity message, CancellationToken cancellationToken)
    {
        await _eventPublisher.PublishAsync(message.EventType, message.Payload, cancellationToken);
    }
}

/// <summary>
/// Interface for publishing events to external message brokers.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync(string eventType, string payload, CancellationToken cancellationToken = default);
}
