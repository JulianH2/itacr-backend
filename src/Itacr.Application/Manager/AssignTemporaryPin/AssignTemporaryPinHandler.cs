using FluentValidation;
using Itacr.Application.Common.Results;
using Itacr.Application.Common.Validation;
using Itacr.Application.Identity;

namespace Itacr.Application.Manager.AssignTemporaryPin;

/// <summary>
/// A manager answers a PIN reset request: the temporary PIN is stored hashed, the lockout is cleared, and the request disappears for
/// every manager. The employee signs in with it as with any PIN. When another manager answered first, the answer is a conflict.
/// </summary>
public sealed class AssignTemporaryPinHandler(
    IStaffAccessRepository staffAccessRepository,
    ICredentialHasher credentialHasher,
    TimeProvider timeProvider,
    IValidator<AssignTemporaryPinCommand> validator)
{
    public async Task<Result> HandleAsync(Guid employeeId, AssignTemporaryPinCommand command, CancellationToken cancellationToken)
    {
        if (await validator.ValidateToErrorAsync(command, cancellationToken) is { } error)
        {
            return error;
        }

        var outcome = await staffAccessRepository.AssignTemporaryPinAsync(
            employeeId,
            credentialHasher.Hash(command.Pin!),
            timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);

        return outcome switch
        {
            AccessRequestOutcome.Applied => Result.Success(),
            AccessRequestOutcome.AlreadyHandled => StaffAccessErrors.PinResetAlreadyHandled,
            _ => StaffAccessErrors.EmployeeNotFound,
        };
    }
}
