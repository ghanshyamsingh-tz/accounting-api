using Accounting.Identity.Domain.Aggregates.UserAccount;
using Accounting.Identity.Domain.Common;

namespace Accounting.Identity.Domain.Events;

/// <summary>
/// Domain event raised when a user successfully authenticates.
/// </summary>
public sealed record UserAuthenticated : DomainEvent
{
    public UserAccountId UserId { get; init; }
    public EmailAddress Email { get; init; }
    public DateTime AuthenticatedAt { get; init; }

    public UserAuthenticated(UserAccountId userId, EmailAddress email)
    {
        UserId = userId;
        Email = email;
        AuthenticatedAt = DateTime.UtcNow;
    }
}
