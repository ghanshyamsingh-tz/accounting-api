using Accounting.Identity.Domain.Aggregates.UserAccount;

namespace Accounting.Identity.Domain.Interfaces;

/// <summary>
/// Repository interface for UserAccount aggregate persistence
/// </summary>
public interface IUserAccountRepository : IRepository<UserAccount, UserAccountId>
{
    /// <summary>
    /// Finds a user account by email address
    /// </summary>
    Task<UserAccount?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if an email address is already registered
    /// </summary>
    Task<bool> ExistsAsync(EmailAddress email, CancellationToken cancellationToken = default);
}
