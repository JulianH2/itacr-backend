using Itacr.Application.Common.Results;
using Itacr.Domain.Identity;

namespace Itacr.Application.Manager.ApproveAccessRequest;

/// <summary>
/// A manager approves a pending account: it becomes Active, so the employee can sign in with the PIN they chose, and the manager is
/// recorded in <c>ApprovedById</c>. When another manager decided first, the answer is a conflict.
/// </summary>
public sealed class ApproveAccessRequestHandler(IStaffAccessRepository staffAccessRepository, TimeProvider timeProvider)
{
    public async Task<Result> HandleAsync(Guid employeeId, Guid managerId, CancellationToken cancellationToken)
    {
        var outcome = await staffAccessRepository.ReviewAccountAsync(
            employeeId,
            EmployeeStatus.Active,
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
