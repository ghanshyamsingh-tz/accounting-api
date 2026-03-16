namespace Accounting.Identity.Infrastructure.Persistence.Entities;

/// <summary>
/// Persistence entity for login attempts.
/// This is a placeholder - will be implemented in Phase 4 (User Story 2).
/// </summary>
public class LoginAttemptEntity
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string AttemptedEmail { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime AttemptedAt { get; set; }
}
