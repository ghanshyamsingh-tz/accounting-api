using Accounting.Identity.Domain.Aggregates.UserAccount;
using Accounting.Identity.Infrastructure.Persistence.Entities;

namespace Accounting.Identity.Infrastructure.Persistence.Mappers;

/// <summary>
/// Maps between UserAccount domain entity and UserAccountEntity persistence entity
/// </summary>
public static class UserAccountMapper
{
    /// <summary>
    /// Maps domain aggregate to persistence entity
    /// </summary>
    public static UserAccountEntity ToEntity(UserAccount aggregate)
    {
        return new UserAccountEntity
        {
            Id = aggregate.Id.Value,
            Email = aggregate.Email.Value,
            PasswordHash = aggregate.PasswordHash.Value,
            FirstName = aggregate.Name.FirstName,
            LastName = aggregate.Name.LastName,
            Status = aggregate.Status.ToString(),
            CreatedAt = aggregate.CreatedAt,
            LastLoginAt = aggregate.LastLoginAt,
            LockedAt = aggregate.LockedAt,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Maps persistence entity to domain aggregate
    /// Uses reflection to bypass private constructor - needed for EF Core hydration
    /// </summary>
    public static UserAccount ToDomain(UserAccountEntity entity)
    {
        var emailResult = EmailAddress.Create(entity.Email);
        if (emailResult.IsFailure)
            throw new InvalidOperationException($"Invalid email in database: {entity.Email}");

        var passwordHash = PasswordHash.FromHash(entity.PasswordHash);

        var nameResult = FullName.Create(entity.FirstName, entity.LastName);
        if (nameResult.IsFailure)
            throw new InvalidOperationException($"Invalid name in database for user {entity.Id}");

        if (!Enum.TryParse<AccountStatus>(entity.Status, out var status))
            throw new InvalidOperationException($"Invalid status in database: {entity.Status}");

        // Use reflection to create UserAccount with private constructor
        var userAccount = (UserAccount)Activator.CreateInstance(
            typeof(UserAccount),
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null,
            new object[]
            {
                UserAccountId.From(entity.Id),
                emailResult.Value,
                passwordHash,
                nameResult.Value
            },
            null)!;

        // Set additional properties using reflection
        var statusProperty = typeof(UserAccount).GetProperty(nameof(UserAccount.Status))!;
        statusProperty.SetValue(userAccount, status);

        var createdAtProperty = typeof(UserAccount).GetProperty(nameof(UserAccount.CreatedAt))!;
        createdAtProperty.SetValue(userAccount, entity.CreatedAt);

        var lastLoginAtProperty = typeof(UserAccount).GetProperty(nameof(UserAccount.LastLoginAt))!;
        lastLoginAtProperty.SetValue(userAccount, entity.LastLoginAt);

        var lockedAtProperty = typeof(UserAccount).GetProperty(nameof(UserAccount.LockedAt))!;
        lockedAtProperty.SetValue(userAccount, entity.LockedAt);

        return userAccount;
    }
}
