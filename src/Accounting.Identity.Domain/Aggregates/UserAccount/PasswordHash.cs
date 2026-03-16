namespace Accounting.Identity.Domain.Aggregates.UserAccount;

/// <summary>
/// Password hash value object using BCrypt.
/// </summary>
public sealed record PasswordHash
{
    public string Value { get; init; }

    private PasswordHash(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Hashes a plain text password using BCrypt.
    /// </summary>
    public static PasswordHash FromPlainText(string plainTextPassword)
    {
        if (string.IsNullOrWhiteSpace(plainTextPassword))
        {
            throw new ArgumentException("Password cannot be empty", nameof(plainTextPassword));
        }

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainTextPassword, workFactor: 12);
        return new PasswordHash(hashedPassword);
    }

    /// <summary>
    /// Creates a PasswordHash from an already hashed password (e.g., from database).
    /// </summary>
    public static PasswordHash FromHash(string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword))
        {
            throw new ArgumentException("Hashed password cannot be empty", nameof(hashedPassword));
        }

        return new PasswordHash(hashedPassword);
    }

    /// <summary>
    /// Verifies a plain text password against this hash.
    /// </summary>
    public bool Verify(string plainTextPassword)
    {
        if (string.IsNullOrWhiteSpace(plainTextPassword))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(plainTextPassword, Value);
        }
        catch
        {
            return false;
        }
    }

    public override string ToString() => "[REDACTED]";
}
