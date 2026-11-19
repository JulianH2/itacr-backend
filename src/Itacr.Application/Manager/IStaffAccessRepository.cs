using Itacr.Domain.Identity;

namespace Itacr.Application.Manager;

/// <summary>An account that asked for access from the login screen and waits for a manager.</summary>
public sealed record PendingAccount(Guid Id, string FullName, EmployeeRole Role, DateTime RequestedAt);

/// <summary>An active employee who asked to reset the PIN and nobody has attended yet.</summary>
public sealed record PendingPinReset(Guid EmployeeId, string FullName, EmployeeRole Role, DateTime RequestedAt);

public sealed record PendingAccessRequests(IReadOnlyList<PendingAccount> Accounts, IReadOnlyList<PendingPinReset> PinResets);

/// <summary>What a write on a request did: it was applied, the request was already handled, or the employee does not exist.</summary>
public enum AccessRequestOutcome
{
    NotFound,
    Applied,
    AlreadyHandled,
}

/// <summary>Reads and answers the requests the manager attends (HU_15): new accounts and PIN resets.</summary>
public interface IStaffAccessRepository
{
    Task<PendingAccessRequests> GetPendingAsync(CancellationToken cancellationToken);

    /// <summary>Moves a pending account to <paramref name="newStatus"/> (Active or Rejected); only an approval records the manager.</summary>
    Task<AccessRequestOutcome> ReviewAccountAsync(Guid employeeId, EmployeeStatus newStatus, Guid managerId, DateTime now, CancellationToken cancellationToken);

    /// <summary>Replaces the PIN of the employee, clears the lockout, and resolves their PIN reset requests, atomically.</summary>
    Task<AccessRequestOutcome> AssignTemporaryPinAsync(Guid employeeId, string pinHash, DateTime now, CancellationToken cancellationToken);
}
