using Accounting.Identity.Domain.Aggregates.UserAccount;
using Accounting.Identity.Domain.Common;

namespace Accounting.Identity.Domain.Events;

/// <summary>
/// Domain event raised when a user account is locked.
/// </summary>
public sealed record AccountLocked : DomainEvent
{
    public UserAccountId UserId { get; init; }
    public string Reason { get; init; }

    public AccountLocked(UserAccountId userId, string reason)
    {
        UserId = userId;
        Reason = reason;
    }
}
