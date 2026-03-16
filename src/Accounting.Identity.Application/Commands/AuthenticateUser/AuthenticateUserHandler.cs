using Accounting.Identity.Domain.Aggregates.UserAccount;
using Accounting.Identity.Domain.Common;
using Accounting.Identity.Domain.Interfaces;
using MediatR;

namespace Accounting.Identity.Application.Commands.AuthenticateUser;

/// <summary>
/// Handler for user authentication command
/// </summary>
public class AuthenticateUserHandler : IRequestHandler<AuthenticateUserCommand, Result<AuthenticationResponse>>
{
    private readonly IUserAccountRepository _repository;

    public AuthenticateUserHandler(IUserAccountRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<AuthenticationResponse>> Handle(AuthenticateUserCommand request, CancellationToken cancellationToken)
    {
        // Validate and create email address
        var emailResult = CreateEmailAddress(request.Email);
        if (emailResult.IsFailure)
            return Result<AuthenticationResponse>.Failure(emailResult.Error);

        // Retrieve user account
        var user = await _repository.GetByEmailAsync(emailResult.Value, cancellationToken);
        if (user is null)
            return Result<AuthenticationResponse>.Failure(new Error("Authentication.InvalidCredentials", "Invalid email or password"));

        // Authenticate user
        var authResult = user.Authenticate(request.Password);
        if (authResult.IsFailure)
            return Result<AuthenticationResponse>.Failure(authResult.Error);

        // Return authentication response
        var response = new AuthenticationResponse(
            Token: authResult.Value.UserId.ToString(),
            ExpiresAt: authResult.Value.ExpiresAt
        );

        return Result<AuthenticationResponse>.Success(response);
    }

    private static Result<EmailAddress> CreateEmailAddress(string email)
    {
        return EmailAddress.Create(email);
    }
}
