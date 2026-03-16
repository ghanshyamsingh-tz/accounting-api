using Accounting.Identity.Domain.Common;

namespace Accounting.Identity.Domain.Aggregates.UserAccount;

/// <summary>
/// Strong-typed identifier for UserAccount aggregate.
/// </summary>
public sealed record UserAccountId(Guid Value)
{
    public static UserAccountId New() => new(Guid.NewGuid());
    
    public static UserAccountId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
