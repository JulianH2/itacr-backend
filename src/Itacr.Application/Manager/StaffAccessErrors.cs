using Itacr.Application.Common.Results;

namespace Itacr.Application.Manager;

/// <summary>The errors of the access requests the manager attends (HU_15).</summary>
public static class StaffAccessErrors
{
    public const string RequestConflictType = "https://itacr.app/problems/access-request-conflict";

    public static readonly Error EmployeeNotFound = new(ErrorKind.NotFound, "Employee not found", ManagerMessages.EmployeeNotFound);

    public static readonly Error AccountAlreadyHandled = new(
        ErrorKind.Conflict,
        "Access request already handled",
        ManagerMessages.AccountAlreadyHandled,
        RequestConflictType);

    public static readonly Error PinResetAlreadyHandled = new(
        ErrorKind.Conflict,
        "PIN reset already handled",
        ManagerMessages.PinResetAlreadyHandled,
        RequestConflictType);
}
