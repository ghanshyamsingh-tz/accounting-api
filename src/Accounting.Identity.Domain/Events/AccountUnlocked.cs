using Accounting.Identity.Domain.Aggregates.UserAccount;
using Accounting.Identity.Domain.Common;

namespace Accounting.Identity.Domain.Events;

/// <summary>
/// Domain event raised when a user account is unlocked.
/// </summary>
public sealed record AccountUnlocked : DomainEvent
{
    public UserAccountId UserId { get; init; }

    public AccountUnlocked(UserAccountId userId)
    {
        UserId = userId;
    }
}
