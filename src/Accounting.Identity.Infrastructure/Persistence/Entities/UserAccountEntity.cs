namespace Accounting.Identity.Infrastructure.Persistence.Entities;

/// <summary>
/// EF Core persistence entity for UserAccount aggregate
/// </summary>
public class UserAccountEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LockedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
