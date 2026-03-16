using Accounting.Identity.Domain.Aggregates.UserAccount;
using Accounting.Identity.Domain.Common;

namespace Accounting.Identity.Domain.Events;

/// <summary>
/// Domain event raised when a user updates their profile.
/// </summary>
public sealed record ProfileUpdated : DomainEvent
{
    public UserAccountId UserId { get; init; }
    public FullName NewName { get; init; }

    public ProfileUpdated(UserAccountId userId, FullName newName)
    {
        UserId = userId;
        NewName = newName;
    }
}
