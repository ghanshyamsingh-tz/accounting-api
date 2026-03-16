using Accounting.Identity.Application.Commands.RegisterUser;
using Accounting.Identity.Domain.Aggregates.UserAccount;
using Accounting.Identity.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace Accounting.Identity.Application.Tests.Commands;

public class RegisterUserHandlerTests
{
    private readonly Mock<IUserAccountRepository> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly RegisterUserHandler _handler;

    public RegisterUserHandlerTests()
    {
        _mockRepository = new Mock<IUserAccountRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _handler = new RegisterUserHandler(_mockRepository.Object, _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_WithValidEmail_ReturnsSuccess()
    {
        // Arrange
        var command = new RegisterUserCommand(
            Email: "test@example.com",
            Password: "SecurePass123!",
            FirstName: "John",
            LastName: "Doe"
        );

        _mockRepository.Setup(r => r.ExistsAsync(It.IsAny<EmailAddress>(), default))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<UserAccount>(), default), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData("missing@domain")]
    [InlineData("@nodomain.com")]
    [InlineData("spaces in@email.com")]
    public async Task Handle_WithInvalidEmail_ReturnsError(string invalidEmail)
    {
        // Arrange
        var command = new RegisterUserCommand(
            Email: invalidEmail,
            Password: "SecurePass123!",
            FirstName: "John",
            LastName: "Doe"
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Match(code => 
            code == "Email.Empty" || code == "Email.Invalid",
            "email should be rejected for invalid format");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<UserAccount>(), default), Times.Never);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("nouppercaseornumber1")]
    [InlineData("NOLOWERCASEORNUMBER1")]
    [InlineData("NoSpecialChar1")]
    [InlineData("NoNumber!")]
    public async Task Handle_WithWeakPassword_ReturnsError(string weakPassword)
    {
        // Arrange
        var command = new RegisterUserCommand(
            Email: "test@example.com",
            Password: weakPassword,
            FirstName: "John",
            LastName: "Doe"
        );

        _mockRepository.Setup(r => r.ExistsAsync(It.IsAny<EmailAddress>(), default))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Match(code => 
            code == "Password.TooShort" || code == "Password.Complexity",
            "password should be rejected for being too weak");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<UserAccount>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_WithExistingEmail_ReturnsError()
    {
        // Arrange
        var command = new RegisterUserCommand(
            Email: "existing@example.com",
            Password: "SecurePass123!",
            FirstName: "John",
            LastName: "Doe"
        );

        _mockRepository.Setup(r => r.ExistsAsync(It.IsAny<EmailAddress>(), default))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.AlreadyExists");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<UserAccount>(), default), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Handle_WithEmptyFirstName_ReturnsError(string? firstName)
    {
        // Arrange
        var command = new RegisterUserCommand(
            Email: "test@example.com",
            Password: "SecurePass123!",
            FirstName: firstName!,
            LastName: "Doe"
        );

        _mockRepository.Setup(r => r.ExistsAsync(It.IsAny<EmailAddress>(), default))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Name.FirstNameEmpty");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<UserAccount>(), default), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Handle_WithEmptyLastName_ReturnsError(string? lastName)
    {
        // Arrange
        var command = new RegisterUserCommand(
            Email: "test@example.com",
            Password: "SecurePass123!",
            FirstName: "John",
            LastName: lastName!
        );

        _mockRepository.Setup(r => r.ExistsAsync(It.IsAny<EmailAddress>(), default))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Name.LastNameEmpty");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<UserAccount>(), default), Times.Never);
    }
}
