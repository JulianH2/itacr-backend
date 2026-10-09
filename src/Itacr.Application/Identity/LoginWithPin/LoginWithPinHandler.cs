using FluentValidation;
using Itacr.Application.Common.Results;
using Itacr.Application.Common.Validation;
using Itacr.Domain.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Itacr.Application.Identity.LoginWithPin;

/// <summary>Signs an employee in with a PIN. Wrong PINs are counted and lock the PIN sign-in after the configured maximum.</summary>
public sealed class LoginWithPinHandler(
    IEmployeeRepository employeeRepository,
    ICredentialHasher credentialHasher,
    ITokenService tokenService,
    TimeProvider timeProvider,
    IOptions<LoginSecurityOptions> loginSecurity,
    IValidator<LoginWithPinCommand> validator,
    ILogger<LoginWithPinHandler> logger)
{
    public async Task<Result<LoginResponse>> HandleAsync(LoginWithPinCommand command, CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } error)
        {
            return error;
        }

        var employee = await employeeRepository.GetByIdAsync(command.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return IdentityErrors.InvalidPin;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (employee.LockoutEndAt > now)
        {
            return IdentityErrors.Locked;
        }

        var verification = credentialHasher.Verify(employee.PinHash, command.Pin);
        if (verification == CredentialVerification.Failed)
        {
            return await RegisterFailedPinAsync(employee, now, cancellationToken);
        }

        var newPinHash = verification == CredentialVerification.SuccessRehashNeeded ? credentialHasher.Hash(command.Pin) : null;
        await employeeRepository.RegisterSuccessfulPinLoginAsync(employee.Id, newPinHash, now, cancellationToken);
        return EmployeeSignIn.Complete(employee, tokenService);
    }

    private async Task<Error> RegisterFailedPinAsync(Employee employee, DateTime now, CancellationToken cancellationToken)
    {
        var settings = loginSecurity.Value;
        var failure = await employeeRepository.RegisterFailedPinLoginAsync(
            employee.Id,
            settings.MaxFailedPinAttempts,
            now.AddMinutes(settings.LockoutMinutes),
            now,
            cancellationToken);

        if (!failure.IsLocked)
        {
            return IdentityErrors.InvalidPin;
        }

        logger.LogWarning("PIN login locked for employee {EmployeeId} until {LockoutEndAt}", employee.Id, failure.LockoutEndAt);
        return IdentityErrors.Locked;
    }
}
