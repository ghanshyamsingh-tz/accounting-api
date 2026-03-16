using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Accounting.Identity.Infrastructure.Persistence.Entities;

namespace Accounting.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for OutboxMessageEntity.
/// Configures table name, column mappings, and indexes following PostgreSQL conventions.
/// </summary>
public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
    {
        // Table name (lowercase_snake_case per PostgreSQL standards)
        builder.ToTable("outbox_messages");

        // Primary key
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .HasColumnName("id")
            .ValueGeneratedNever(); // Guid generated in application code

        // Event type
        builder.Property(o => o.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(500)
            .IsRequired();

        // Payload (JSON)
        builder.Property(o => o.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb") // Use PostgreSQL JSONB for efficient querying
            .IsRequired();

        // Published flag
        builder.Property(o => o.Published)
            .HasColumnName("published")
            .HasDefaultValue(false)
            .IsRequired();

        // Published timestamp
        builder.Property(o => o.PublishedAt)
            .HasColumnName("published_at");

        // Created timestamp
        builder.Property(o => o.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Retry tracking
        builder.Property(o => o.AttemptCount)
            .HasColumnName("attempt_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(o => o.LastAttemptAt)
            .HasColumnName("last_attempt_at");

        builder.Property(o => o.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(2000);

        // Indexes for efficient querying
        // Index for finding unpublished messages (most common query)
        builder.HasIndex(o => new { o.Published, o.CreatedAt })
            .HasDatabaseName("ix_outbox_messages_published_created_at")
            .HasFilter("published = false");

        // Index for finding messages by event type
        builder.HasIndex(o => o.EventType)
            .HasDatabaseName("ix_outbox_messages_event_type");

        // Index for cleanup queries (finding old published messages to archive)
        builder.HasIndex(o => new { o.Published, o.PublishedAt })
            .HasDatabaseName("ix_outbox_messages_published_published_at");
    }
}
