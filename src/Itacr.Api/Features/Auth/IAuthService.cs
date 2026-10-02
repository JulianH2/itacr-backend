using Itacr.Api.Features.Auth.Requests;
using Itacr.Api.Features.Auth.Responses;
using Itacr.Api.Features.Employees;

namespace Itacr.Api.Features.Auth;

public interface IAuthService
{
    Task<IReadOnlyList<EmployeeOptionResponse>> GetActiveEmployeesAsync(EmployeeRole role, CancellationToken cancellationToken);

    Task<LoginResult> LoginWithPinAsync(PinLoginRequest request, CancellationToken cancellationToken);

    Task<LoginResult> LoginWithPasswordAsync(PasswordLoginRequest request, CancellationToken cancellationToken);

    Task<bool> RequestPinResetAsync(Guid employeeId, CancellationToken cancellationToken);
}
