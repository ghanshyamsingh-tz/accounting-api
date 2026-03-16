namespace Accounting.Identity.Application.Commands.AuthenticateUser;

/// <summary>
/// Response for successful authentication
/// </summary>
public record AuthenticationResponse(
    string Token,
    DateTime ExpiresAt
);
