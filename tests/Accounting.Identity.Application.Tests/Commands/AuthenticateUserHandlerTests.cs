using Accounting.Identity.Application.Commands.AuthenticateUser;
using Accounting.Identity.Domain.Aggregates.UserAccount;
using Accounting.Identity.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace Accounting.Identity.Application.Tests.Commands;

public class AuthenticateUserHandlerTests
{
    private readonly Mock<IUserAccountRepository> _mockRepository;
    private readonly AuthenticateUserHandler _handler;

    public AuthenticateUserHandlerTests()
    {
        _mockRepository = new Mock<IUserAccountRepository>();
        _handler = new AuthenticateUserHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var email = EmailAddress.Create("test@example.com").Value;
        var password = "SecurePass123!";
        var user = UserAccount.Register(email, password, FullName.Create("John", "Doe").Value).Value;

        var command = new AuthenticateUserCommand(
            Email: "test@example.com",
            Password: password
        );

        _mockRepository.Setup(r => r.GetByEmailAsync(It.IsAny<EmailAddress>(), default))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().NotBeNullOrEmpty();
        result.Value.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_WithInvalidEmail_ReturnsError()
    {
        // Arrange
        var command = new AuthenticateUserCommand(
            Email: "nonexistent@example.com",
            Password: "SecurePass123!"
        );

        _mockRepository.Setup(r => r.GetByEmailAsync(It.IsAny<EmailAddress>(), default))
            .ReturnsAsync((UserAccount?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Authentication.InvalidCredentials");
    }

    [Fact]
    public async Task Handle_WithInvalidPassword_ReturnsError()
    {
        // Arrange
        var email = EmailAddress.Create("test@example.com").Value;
        var correctPassword = "SecurePass123!";
        var user = UserAccount.Register(email, correctPassword, FullName.Create("John", "Doe").Value).Value;

        var command = new AuthenticateUserCommand(
            Email: "test@example.com",
            Password: "WrongPassword123!"
        );

        _mockRepository.Setup(r => r.GetByEmailAsync(It.IsAny<EmailAddress>(), default))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Authentication.InvalidCredentials");
    }

    [Fact]
    public async Task Handle_WithLockedAccount_ReturnsError()
    {
        // Arrange
        var email = EmailAddress.Create("test@example.com").Value;
        var password = "SecurePass123!";
        var user = UserAccount.Register(email, password, FullName.Create("John", "Doe").Value).Value;
        user.Lock("Too many failed attempts");

        var command = new AuthenticateUserCommand(
            Email: "test@example.com",
            Password: password
        );

        _mockRepository.Setup(r => r.GetByEmailAsync(It.IsAny<EmailAddress>(), default))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Authentication.AccountLocked");
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    public async Task Handle_WithInvalidEmailFormat_ReturnsError(string invalidEmail)
    {
        // Arrange
        var command = new AuthenticateUserCommand(
            Email: invalidEmail,
            Password: "SecurePass123!"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Match(code => 
            code == "Email.Empty" || code == "Email.Invalid",
            "email should be rejected for invalid format");
        _mockRepository.Verify(r => r.GetByEmailAsync(It.IsAny<EmailAddress>(), default), Times.Never);
    }
}
