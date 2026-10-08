using Itacr.Application.Identity.Requests;
using Itacr.Application.Identity.Responses;
using Itacr.Domain.Identity;

namespace Itacr.Application.Identity;

public interface IAuthService
{
    Task<IReadOnlyList<EmployeeOptionResponse>> GetActiveEmployeesAsync(EmployeeRole role, CancellationToken cancellationToken);

    Task<LoginResult> LoginWithPinAsync(PinLoginRequest request, CancellationToken cancellationToken);

    Task<LoginResult> LoginWithPasswordAsync(PasswordLoginRequest request, CancellationToken cancellationToken);

    Task<bool> RequestPinResetAsync(Guid employeeId, CancellationToken cancellationToken);
}
