namespace Accounting.Identity.Infrastructure.Persistence.Entities;

/// <summary>
/// Persistence entity for security events.
/// This is a placeholder - will be implemented in Phase 5 (User Story 3).
/// </summary>
public class SecurityEventEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public bool Resolved { get; set; }
    public DateTime CreatedAt { get; set; }
}
