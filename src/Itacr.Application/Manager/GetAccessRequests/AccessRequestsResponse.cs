using Itacr.Domain.Identity;

namespace Itacr.Application.Manager.GetAccessRequests;

/// <summary>An account that waits for the manager's decision.</summary>
/// <param name="Id">Employee identifier, used to approve or reject.</param>
/// <param name="FullName">Name the employee typed.</param>
/// <param name="Role">Waiter or Kitchen.</param>
/// <param name="RequestedAt">UTC instant when the account was asked for.</param>
public sealed record PendingAccountResponse(Guid Id, string FullName, EmployeeRole Role, DateTime RequestedAt);

/// <summary>An employee waiting for a temporary PIN.</summary>
/// <param name="EmployeeId">Employee identifier, used to assign the PIN.</param>
/// <param name="FullName">Name of the employee.</param>
/// <param name="Role">Role of the employee.</param>
/// <param name="RequestedAt">UTC instant of the last request.</param>
public sealed record PendingPinResetResponse(Guid EmployeeId, string FullName, EmployeeRole Role, DateTime RequestedAt);

/// <summary>What the manager has to attend; both lists are empty when there is nothing pending.</summary>
/// <param name="Accounts">Accounts that asked for access, oldest first.</param>
/// <param name="PinResets">Employees that asked to reset their PIN, oldest first.</param>
public sealed record AccessRequestsResponse(IReadOnlyList<PendingAccountResponse> Accounts, IReadOnlyList<PendingPinResetResponse> PinResets);
