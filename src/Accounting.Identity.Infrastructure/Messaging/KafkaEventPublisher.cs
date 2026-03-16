using Accounting.Identity.Infrastructure.Outbox;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Accounting.Identity.Infrastructure.Messaging;

/// <summary>
/// Kafka-based event publisher for domain events.
/// Publishes events to Kafka topics for consumption by other services.
/// </summary>
public class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;
    private readonly string _defaultTopic;

    public KafkaEventPublisher(
        ILogger<KafkaEventPublisher> logger,
        string bootstrapServers,
        string defaultTopic = "identity.domain-events")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultTopic = defaultTopic;

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All, // Wait for all in-sync replicas to acknowledge
            EnableIdempotence = true, // Ensure exactly-once delivery
            MaxInFlight = 5,
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 100,
            RequestTimeoutMs = 30000,
            LingerMs = 10, // Batch messages for up to 10ms for better throughput
            CompressionType = CompressionType.Snappy
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
            {
                _logger.LogError("Kafka error: {Code} - {Reason}", error.Code, error.Reason);
            })
            .Build();

        _logger.LogInformation("Kafka producer initialized with bootstrap servers: {BootstrapServers}", bootstrapServers);
    }

    /// <inheritdoc />
    public async Task PublishAsync(string eventType, string payload, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(), // Use event ID as key for partitioning
                Value = payload,
                Headers = new Headers
                {
                    { "event-type", System.Text.Encoding.UTF8.GetBytes(eventType) },
                    { "timestamp", System.Text.Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) },
                    { "source", System.Text.Encoding.UTF8.GetBytes("identity-service") }
                }
            };

            var result = await _producer.ProduceAsync(_defaultTopic, message, cancellationToken);

            _logger.LogInformation(
                "Published event {EventType} to Kafka topic {Topic} at offset {Offset}",
                eventType,
                result.Topic,
                result.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish event {EventType} to Kafka: {ErrorCode} - {ErrorReason}",
                eventType,
                ex.Error.Code,
                ex.Error.Reason);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error publishing event {EventType} to Kafka", eventType);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
        _logger.LogInformation("Kafka producer disposed");
    }
}

/// <summary>
/// Extension methods for registering Kafka event publisher.
/// </summary>
public static class KafkaEventPublisherExtensions
{
    public static IServiceCollection AddKafkaEventPublisher(
        this IServiceCollection services,
        string bootstrapServers,
        string defaultTopic = "identity.domain-events")
    {
        services.AddSingleton<IEventPublisher>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<KafkaEventPublisher>>();
            return new KafkaEventPublisher(logger, bootstrapServers, defaultTopic);
        });

        return services;
    }
}
