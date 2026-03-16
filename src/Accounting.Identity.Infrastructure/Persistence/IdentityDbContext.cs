using Microsoft.EntityFrameworkCore;
using Accounting.Identity.Infrastructure.Persistence.Entities;

namespace Accounting.Identity.Infrastructure.Persistence;

/// <summary>
/// Database context for the Identity bounded context.
/// Uses PostgreSQL with the 'identity' schema for isolation from other bounded contexts.
/// </summary>
public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// User accounts in the system.
    /// </summary>
    public DbSet<UserAccountEntity> UserAccounts => Set<UserAccountEntity>();

    /// <summary>
    /// Login attempts (successful and failed).
    /// </summary>
    public DbSet<LoginAttemptEntity> LoginAttempts => Set<LoginAttemptEntity>();

    /// <summary>
    /// Security events requiring attention.
    /// </summary>
    public DbSet<SecurityEventEntity> SecurityEvents => Set<SecurityEventEntity>();

    /// <summary>
    /// Outbox messages for reliable event publishing.
    /// </summary>
    public DbSet<OutboxMessageEntity> OutboxMessages => Set<OutboxMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set default schema to 'identity' for all entities
        modelBuilder.HasDefaultSchema("identity");

        // Apply all entity configurations from the same assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }

    /// <summary>
    /// Override SaveChanges to ensure UTC timestamps.
    /// </summary>
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    /// <summary>
    /// Override SaveChangesAsync to ensure UTC timestamps.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Updates created_at and updated_at timestamps for tracked entities.
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is ITimestampedEntity timestampedEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    timestampedEntity.CreatedAt = DateTime.UtcNow;
                }

                timestampedEntity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}

/// <summary>
/// Interface for entities that track creation and update timestamps.
/// </summary>
public interface ITimestampedEntity
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}
