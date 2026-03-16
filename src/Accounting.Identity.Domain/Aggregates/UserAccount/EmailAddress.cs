using Accounting.Identity.Domain.Common;
using System.Text.RegularExpressions;

namespace Accounting.Identity.Domain.Aggregates.UserAccount;

/// <summary>
/// Email address value object with validation.
/// </summary>
public sealed record EmailAddress
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; init; }

    private EmailAddress(string value)
    {
        Value = value.ToLowerInvariant();
    }

    /// <summary>
    /// Creates an email address with validation.
    /// </summary>
    public static Result<EmailAddress> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result<EmailAddress>.Failure("Email.Empty", "Email address cannot be empty");
        }

        email = email.Trim();

        if (!EmailRegex.IsMatch(email))
        {
            return Result<EmailAddress>.Failure("Email.Invalid", "Email address format is invalid");
        }

        if (email.Length > 255)
        {
            return Result<EmailAddress>.Failure("Email.TooLong", "Email address cannot exceed 255 characters");
        }

        return new EmailAddress(email);
    }

    public override string ToString() => Value;
}
