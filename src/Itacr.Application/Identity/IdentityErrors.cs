using Itacr.Application.Common.Results;

namespace Itacr.Application.Identity;

/// <summary>The sign-in errors. Their types, titles, and status codes are the contract itacr-frontend switches on.</summary>
public static class IdentityErrors
{
    public static readonly Error InvalidPin = new(ErrorKind.Unauthorized, "Invalid credentials", AuthProblems.InvalidPin, AuthProblems.InvalidCredentialsType);

    public static readonly Error InvalidPassword = new(ErrorKind.Unauthorized, "Invalid credentials", AuthProblems.InvalidPassword, AuthProblems.InvalidCredentialsType);

    public static readonly Error PendingApproval = new(ErrorKind.Forbidden, "Account pending approval", AuthProblems.PendingApproval, AuthProblems.PendingApprovalType);

    public static readonly Error Inactive = new(ErrorKind.Forbidden, "Account not active", AuthProblems.Inactive, AuthProblems.InactiveType);

    public static readonly Error Locked = new(ErrorKind.Locked, "Account locked", AuthProblems.Locked, AuthProblems.LockedType);

    public static readonly Error EmployeeNotFound = new(ErrorKind.NotFound, "Employee not found", AuthProblems.EmployeeNotFound);
}
