using Accounting.Identity.Domain.Aggregates.UserAccount;
using Accounting.Identity.Domain.Events;
using FluentAssertions;
using Xunit;

namespace Accounting.Identity.Domain.Tests.Aggregates;

/// <summary>
/// Domain tests for UserAccount aggregate.
/// Tests enforce business rules and invariants at the domain level.
/// </summary>
public class UserAccountTests
{
    [Fact]
    public void Register_WithValidData_ShouldCreateUserAccount()
    {
        // Arrange
        var email = EmailAddress.Create("test@example.com").Value;
        var password = "SecurePassword123!";
        var fullName = FullName.Create("John", "Doe").Value;

        // Act
        var result = UserAccount.Register(email, password, fullName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(email);
        result.Value.Name.Should().Be(fullName);
        result.Value.Status.Should().Be(AccountStatus.PendingVerification);
        result.Value.DomainEvents.Should().ContainSingle(e => e is UserRegistered);
    }

    [Theory]
    [InlineData("short")]           // Too short (< 8 chars)
    [InlineData("alllowercase")]    // No uppercase
    [InlineData("ALLUPPERCASE")]    // No lowercase
    [InlineData("NoNumbers!")]      // No numbers
    [InlineData("NoSpecialChar1")]  // No special characters
    public void Register_WithWeakPassword_ShouldReturnFailure(string weakPassword)
    {
        // Arrange
        var email = EmailAddress.Create("test@example.com").Value;
        var fullName = FullName.Create("John", "Doe").Value;

        // Act
        var result = UserAccount.Register(email, weakPassword, fullName);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Password");
    }

    [Fact]
    public void Register_WithInvalidEmail_ShouldReturnFailure()
    {
        // Arrange
        var emailResult = EmailAddress.Create("invalid-email");
        
        // Assert
        emailResult.IsFailure.Should().BeTrue();
        emailResult.Error.Code.Should().Contain("Email");
    }

    [Fact]
    public void Authenticate_WithCorrectPassword_ShouldSucceed()
    {
        // Arrange
        var email = EmailAddress.Create("test@example.com").Value;
        var password = "SecurePassword123!";
        var fullName = FullName.Create("John", "Doe").Value;
        
        var userAccount = UserAccount.Register(email, password, fullName).Value;
        userAccount.ClearDomainEvents(); // Clear registration event
        
        // Unlock account for testing (normally happens after email verification)
        userAccount.Unlock();

        // Act
        var result = userAccount.Authenticate(password);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        userAccount.DomainEvents.Should().ContainSingle(e => e is UserAuthenticated);
        userAccount.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public void Authenticate_WithIncorrectPassword_ShouldFail()
    {
        // Arrange
        var email = EmailAddress.Create("test@example.com").Value;
        var password = "SecurePassword123!";
        var fullName = FullName.Create("John", "Doe").Value;
        
        var userAccount = UserAccount.Register(email, password, fullName).Value;
        userAccount.Unlock();

        // Act
        var result = userAccount.Authenticate("WrongPassword123!");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Authentication.InvalidCredentials");
    }

    [Fact]
    public void Authenticate_WhenAccountIsLocked_ShouldFail()
    {
        // Arrange
        var email = EmailAddress.Create("test@example.com").Value;
        var password = "SecurePassword123!";
        var fullName = FullName.Create("John", "Doe").Value;
        
        var userAccount = UserAccount.Register(email, password, fullName).Value;
        userAccount.Lock("Suspicious activity detected");

        // Act
        var result = userAccount.Authenticate(password);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Authentication.AccountLocked");
        userAccount.Status.Should().Be(AccountStatus.Locked);
    }

    [Fact]
    public void UpdateProfile_WithValidData_ShouldSucceed()
    {
        // Arrange
        var email = EmailAddress.Create("test@example.com").Value;
        var password = "SecurePassword123!";
        var fullName = FullName.Create("John", "Doe").Value;
        
        var userAccount = UserAccount.Register(email, password, fullName).Value;
        userAccount.ClearDomainEvents();

        var newName = FullName.Create("Jane", "Smith").Value;

        // Act
        var result = userAccount.UpdateProfile(newName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        userAccount.Name.Should().Be(newName);
        userAccount.DomainEvents.Should().ContainSingle(e => e is ProfileUpdated);
    }

    [Fact]
    public void Lock_ShouldChangeStatusToLocked()
    {
        // Arrange
        var email = EmailAddress.Create("test@example.com").Value;
        var password = "SecurePassword123!";
        var fullName = FullName.Create("John", "Doe").Value;
        
        var userAccount = UserAccount.Register(email, password, fullName).Value;
        userAccount.ClearDomainEvents();

        // Act
        var result = userAccount.Lock("Security concern");

        // Assert
        result.IsSuccess.Should().BeTrue();
        userAccount.Status.Should().Be(AccountStatus.Locked);
        userAccount.LockedAt.Should().NotBeNull();
        userAccount.DomainEvents.Should().ContainSingle(e => e is AccountLocked);
    }

    [Fact]
    public void Unlock_ShouldChangeStatusToActive()
    {
        // Arrange
        var email = EmailAddress.Create("test@example.com").Value;
        var password = "SecurePassword123!";
        var fullName = FullName.Create("John", "Doe").Value;
        
        var userAccount = UserAccount.Register(email, password, fullName).Value;
        userAccount.Lock("Test lock");
        userAccount.ClearDomainEvents();

        // Act
        var result = userAccount.Unlock();

        // Assert
        result.IsSuccess.Should().BeTrue();
        userAccount.Status.Should().Be(AccountStatus.Active);
        userAccount.LockedAt.Should().BeNull();
        userAccount.DomainEvents.Should().ContainSingle(e => e is AccountUnlocked);
    }

    [Fact]
    public void EmailAddress_WithValidEmail_ShouldCreate()
    {
        // Act
        var result = EmailAddress.Create("test@example.com");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("test@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    [InlineData("test @example.com")]
    public void EmailAddress_WithInvalidEmail_ShouldFail(string invalidEmail)
    {
        // Act
        var result = EmailAddress.Create(invalidEmail);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Email");
    }

    [Fact]
    public void PasswordHash_ShouldHashPassword()
    {
        // Arrange
        var plainPassword = "SecurePassword123!";

        // Act
        var hashedPassword = PasswordHash.FromPlainText(plainPassword);

        // Assert
        hashedPassword.Should().NotBeNull();
        hashedPassword.Value.Should().NotBe(plainPassword);
        hashedPassword.Value.Should().StartWith("$2"); // BCrypt prefix
    }

    [Fact]
    public void PasswordHash_ShouldVerifyCorrectPassword()
    {
        // Arrange
        var plainPassword = "SecurePassword123!";
        var hashedPassword = PasswordHash.FromPlainText(plainPassword);

        // Act
        var isValid = hashedPassword.Verify(plainPassword);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void PasswordHash_ShouldRejectIncorrectPassword()
    {
        // Arrange
        var plainPassword = "SecurePassword123!";
        var hashedPassword = PasswordHash.FromPlainText(plainPassword);

        // Act
        var isValid = hashedPassword.Verify("WrongPassword123!");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void FullName_WithValidNames_ShouldCreate()
    {
        // Act
        var result = FullName.Create("John", "Doe");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be("John");
        result.Value.LastName.Should().Be("Doe");
        result.Value.DisplayName.Should().Be("John Doe");
    }

    [Theory]
    [InlineData("", "Doe")]
    [InlineData("John", "")]
    [InlineData("", "")]
    public void FullName_WithEmptyNames_ShouldFail(string firstName, string lastName)
    {
        // Act
        var result = FullName.Create(firstName, lastName);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Name");
    }
}
