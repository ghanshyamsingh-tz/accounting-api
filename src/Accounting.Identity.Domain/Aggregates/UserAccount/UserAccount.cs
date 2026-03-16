using Accounting.Identity.Domain.Common;
using Accounting.Identity.Domain.Events;
using System.Text.RegularExpressions;

namespace Accounting.Identity.Domain.Aggregates.UserAccount;

/// <summary>
/// UserAccount aggregate root.
/// Represents a user in the system with authentication and profile management capabilities.
/// </summary>
public sealed class UserAccount : AggregateRoot<UserAccountId>
{
    private static readonly Regex PasswordRegex = new(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        RegexOptions.Compiled);

    public EmailAddress Email { get; private set; }
    public PasswordHash PasswordHash { get; private set; }
    public FullName Name { get; private set; }
    public AccountStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public DateTime? LockedAt { get; private set; }

    // Private constructor for EF Core
    private UserAccount()
    {
        Id = null!;
        Email = null!;
        PasswordHash = null!;
        Name = null!;
    }

    private UserAccount(
        UserAccountId id,
        EmailAddress email,
        PasswordHash passwordHash,
        FullName name)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        Name = name;
        Status = AccountStatus.PendingVerification;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Registers a new user account.
    /// Static factory method that enforces business rules.
    /// </summary>
    public static Result<UserAccount> Register(
        EmailAddress email,
        string plainTextPassword,
        FullName name)
    {
        // Validate password complexity
        var passwordValidation = ValidatePasswordComplexity(plainTextPassword);
        if (passwordValidation.IsFailure)
        {
            return Result<UserAccount>.Failure(passwordValidation.Error);
        }

        // Hash the password
        var passwordHash = PasswordHash.FromPlainText(plainTextPassword);

        // Create the user account
        var userAccount = new UserAccount(
            UserAccountId.New(),
            email,
            passwordHash,
            name);

        // Raise domain event
        userAccount.RaiseDomainEvent(new UserRegistered(
            userAccount.Id,
            userAccount.Email,
            userAccount.Name));

        return userAccount;
    }

    /// <summary>
    /// Authenticates a user with their password.
    /// </summary>
    public Result<AuthenticationToken> Authenticate(string plainTextPassword)
    {
        // Check if account is locked
        if (Status == AccountStatus.Locked)
        {
            return Result<AuthenticationToken>.Failure(
                "Authentication.AccountLocked",
                "Account is locked. Please contact support.");
        }

        // Check if account is active or pending verification
        if (Status != AccountStatus.Active && Status != AccountStatus.PendingVerification)
        {
            return Result<AuthenticationToken>.Failure(
                "Authentication.AccountNotActive",
                "Account is not active.");
        }

        // Verify password
        if (!PasswordHash.Verify(plainTextPassword))
        {
            return Result<AuthenticationToken>.Failure(
                "Authentication.InvalidCredentials",
                "Invalid email or password.");
        }

        // Update last login timestamp
        LastLoginAt = DateTime.UtcNow;

        // Raise domain event
        RaiseDomainEvent(new UserAuthenticated(Id, Email));

        // Generate authentication token
        var token = new AuthenticationToken(
            Id,
            Email,
            Name,
            DateTime.UtcNow.AddHours(1)); // Token expires in 1 hour

        return Result<AuthenticationToken>.Success(token);
    }

    /// <summary>
    /// Updates the user's profile information.
    /// </summary>
    public Result UpdateProfile(FullName newName)
    {
        Name = newName;

        RaiseDomainEvent(new ProfileUpdated(Id, newName));

        return Result.Success();
    }

    /// <summary>
    /// Locks the account.
    /// </summary>
    public Result Lock(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure("Lock.ReasonRequired", "A reason must be provided for locking the account.");
        }

        Status = AccountStatus.Locked;
        LockedAt = DateTime.UtcNow;

        RaiseDomainEvent(new AccountLocked(Id, reason));

        return Result.Success();
    }

    /// <summary>
    /// Unlocks the account.
    /// </summary>
    public Result Unlock()
    {
        if (Status != AccountStatus.Locked)
        {
            return Result.Failure("Unlock.NotLocked", "Account is not locked.");
        }

        Status = AccountStatus.Active;
        LockedAt = null;

        RaiseDomainEvent(new AccountUnlocked(Id));

        return Result.Success();
    }

    /// <summary>
    /// Validates password complexity requirements.
    /// - At least 8 characters
    /// - At least one uppercase letter
    /// - At least one lowercase letter
    /// - At least one digit
    /// - At least one special character
    /// </summary>
    private static Result ValidatePasswordComplexity(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return Result.Failure("Password.Empty", "Password cannot be empty.");
        }

        if (password.Length < 8)
        {
            return Result.Failure(
                "Password.TooShort",
                "Password must be at least 8 characters long.");
        }

        if (!PasswordRegex.IsMatch(password))
        {
            return Result.Failure(
                "Password.Complexity",
                "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character.");
        }

        return Result.Success();
    }
}

/// <summary>
/// Authentication token returned after successful authentication.
/// </summary>
public sealed record AuthenticationToken(
    UserAccountId UserId,
    EmailAddress Email,
    FullName Name,
    DateTime ExpiresAt);
