using Accounting.Identity.Domain.Common;
using MediatR;

namespace Accounting.Identity.Application.Commands.AuthenticateUser;

/// <summary>
/// Command to authenticate a user
/// </summary>
public record AuthenticateUserCommand(
    string Email,
    string Password
) : IRequest<Result<AuthenticationResponse>>;
