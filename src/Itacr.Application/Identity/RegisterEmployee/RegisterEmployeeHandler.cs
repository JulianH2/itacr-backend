using FluentValidation;
using Itacr.Application.Common.Results;
using Itacr.Application.Common.Validation;
using Itacr.Domain.Identity;

namespace Itacr.Application.Identity.RegisterEmployee;

/// <summary>
/// A new employee asks for an account: it is saved as pending approval, without a password, with the hash of the PIN and the time
/// the privacy notice was accepted. Nothing gives access until a manager approves it (the sign-in answers "pending approval").
/// </summary>
public sealed class RegisterEmployeeHandler(
    IEmployeeRepository employeeRepository,
    ICredentialHasher credentialHasher,
    TimeProvider timeProvider,
    IValidator<RegisterEmployeeCommand> validator)
{
    public async Task<Result> HandleAsync(RegisterEmployeeCommand command, CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } error)
        {
            return error;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var employee = new Employee
        {
            FullName = command.FullName!.Trim(),
            Email = command.Email!.Trim(),
            Phone = command.Phone!.Trim(),
            Role = command.Role!.Value,
            PinHash = credentialHasher.Hash(command.Pin!),
            Status = EmployeeStatus.PendingApproval,
            PrivacyNoticeAcceptedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
        };

        return await employeeRepository.RegisterAsync(employee, cancellationToken)
            ? Result.Success()
            : IdentityErrors.AccountAlreadyExists;
    }
}
