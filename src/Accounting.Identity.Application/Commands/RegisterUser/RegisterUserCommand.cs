using Accounting.Identity.Domain.Common;
using MediatR;

namespace Accounting.Identity.Application.Commands.RegisterUser;

/// <summary>
/// Command to register a new user account
/// </summary>
public record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName
) : IRequest<Result<Guid>>;
