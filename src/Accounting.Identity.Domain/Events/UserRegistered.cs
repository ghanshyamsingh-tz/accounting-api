using Accounting.Identity.Domain.Aggregates.UserAccount;
using Accounting.Identity.Domain.Common;

namespace Accounting.Identity.Domain.Events;

/// <summary>
/// Domain event raised when a new user registers.
/// </summary>
public sealed record UserRegistered : DomainEvent
{
    public UserAccountId UserId { get; init; }
    public EmailAddress Email { get; init; }
    public FullName Name { get; init; }

    public UserRegistered(UserAccountId userId, EmailAddress email, FullName name)
    {
        UserId = userId;
        Email = email;
        Name = name;
    }
}
