namespace Accounting.Identity.Domain.Aggregates.UserAccount;

/// <summary>
/// User account status enum.
/// </summary>
public enum AccountStatus
{
    /// <summary>
    /// Account created but email not verified yet.
    /// </summary>
    PendingVerification = 0,

    /// <summary>
    /// Account is active and can be used.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Account is locked due to security concerns.
    /// </summary>
    Locked = 2,

    /// <summary>
    /// Account is administratively suspended.
    /// </summary>
    Suspended = 3,

    /// <summary>
    /// Account is permanently closed.
    /// </summary>
    Closed = 4
}
