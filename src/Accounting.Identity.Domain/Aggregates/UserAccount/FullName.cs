using Accounting.Identity.Domain.Common;

namespace Accounting.Identity.Domain.Aggregates.UserAccount;

/// <summary>
/// Full name value object.
/// </summary>
public sealed record FullName
{
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public string DisplayName => $"{FirstName} {LastName}";

    private FullName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    /// <summary>
    /// Creates a full name with validation.
    /// </summary>
    public static Result<FullName> Create(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return Result<FullName>.Failure("Name.FirstNameEmpty", "First name cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return Result<FullName>.Failure("Name.LastNameEmpty", "Last name cannot be empty");
        }

        firstName = firstName.Trim();
        lastName = lastName.Trim();

        if (firstName.Length > 100)
        {
            return Result<FullName>.Failure("Name.FirstNameTooLong", "First name cannot exceed 100 characters");
        }

        if (lastName.Length > 100)
        {
            return Result<FullName>.Failure("Name.LastNameTooLong", "Last name cannot exceed 100 characters");
        }

        return new FullName(firstName, lastName);
    }

    public override string ToString() => DisplayName;
}
