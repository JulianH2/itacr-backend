using Itacr.Application.Common.Results;
using Itacr.Domain.Identity;

namespace Itacr.Application.Manager.RejectAccessRequest;

/// <summary>
/// A manager rejects a pending account: it becomes Rejected and the employee cannot sign in. The row is kept, so its email stays taken
/// and the person cannot ask again with it. When another manager decided first, the answer is a conflict.
/// </summary>
public sealed class RejectAccessRequestHandler(IStaffAccessRepository staffAccessRepository, TimeProvider timeProvider)
{
    public async Task<Result> HandleAsync(Guid employeeId, Guid managerId, CancellationToken cancellationToken)
    {
        var outcome = await staffAccessRepository.ReviewAccountAsync(
            employeeId,
            EmployeeStatus.Rejected,
            managerId,
            timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);

        return outcome switch
        {
            AccessRequestOutcome.Applied => Result.Success(),
            AccessRequestOutcome.AlreadyHandled => StaffAccessErrors.AccountAlreadyHandled,
            _ => StaffAccessErrors.EmployeeNotFound,
        };
    }
}
