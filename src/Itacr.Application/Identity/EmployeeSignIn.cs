using Itacr.Application.Common.Results;
using Itacr.Domain.Identity;

namespace Itacr.Application.Identity;

/// <summary>The last step of every sign-in, shared by the PIN and the password use cases: only an active employee gets a token.</summary>
internal static class EmployeeSignIn
{
    public static Result<LoginResponse> Complete(Employee employee, ITokenService tokenService) => employee.Status switch
    {
        EmployeeStatus.Active => tokenService.CreateLogin(employee),
        EmployeeStatus.PendingApproval => IdentityErrors.PendingApproval,
        _ => IdentityErrors.Inactive,
    };
}
