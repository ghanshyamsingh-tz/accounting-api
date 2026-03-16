using Accounting.Identity.Domain.Aggregates.UserAccount;
using Accounting.Identity.Domain.Common;
using Accounting.Identity.Domain.Interfaces;
using MediatR;

namespace Accounting.Identity.Application.Commands.RegisterUser;

/// <summary>
/// Handler for user registration command
/// </summary>
public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, Result<Guid>>
{
    private readonly IUserAccountRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterUserHandler(IUserAccountRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // Validate and create email address
        var emailResult = CreateEmailAddress(request.Email);
        if (emailResult.IsFailure)
            return Result<Guid>.Failure(emailResult.Error);

        // Validate and create full name
        var nameResult = CreateFullName(request.FirstName, request.LastName);
        if (nameResult.IsFailure)
            return Result<Guid>.Failure(nameResult.Error);

        // Check if email already exists
        var exists = await _repository.ExistsAsync(emailResult.Value, cancellationToken);
        if (exists)
            return Result<Guid>.Failure(new Error("Email.AlreadyExists", "Email address is already registered"));

        // Register new user account
        var registerResult = UserAccount.Register(emailResult.Value, request.Password, nameResult.Value);
        if (registerResult.IsFailure)
            return Result<Guid>.Failure(registerResult.Error);

        // Persist user account
        await _repository.AddAsync(registerResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(registerResult.Value.Id.Value);
    }

    private static Result<EmailAddress> CreateEmailAddress(string email)
    {
        return EmailAddress.Create(email);
    }

    private static Result<FullName> CreateFullName(string firstName, string lastName)
    {
        return FullName.Create(firstName, lastName);
    }
}
