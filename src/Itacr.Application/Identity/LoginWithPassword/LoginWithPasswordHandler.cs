using FluentValidation;
using Itacr.Application.Common.Results;
using Itacr.Application.Common.Validation;

namespace Itacr.Application.Identity.LoginWithPassword;

/// <summary>Signs an employee in with an email and a password.</summary>
public sealed class LoginWithPasswordHandler(
    IEmployeeRepository employeeRepository,
    ICredentialHasher credentialHasher,
    ITokenService tokenService,
    IValidator<LoginWithPasswordCommand> validator)
{
    public async Task<Result<LoginResponse>> HandleAsync(LoginWithPasswordCommand command, CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } error)
        {
            return error;
        }

        var employee = await employeeRepository.GetByEmailAsync(command.Email.Trim(), cancellationToken);
        if (employee?.PasswordHash is null
            || credentialHasher.Verify(employee.PasswordHash, command.Password) == CredentialVerification.Failed)
        {
            return IdentityErrors.InvalidPassword;
        }

        return EmployeeSignIn.Complete(employee, tokenService);
    }
}
