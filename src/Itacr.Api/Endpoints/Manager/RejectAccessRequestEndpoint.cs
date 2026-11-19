using System.Security.Claims;
using Itacr.Api.Common.Http;
using Itacr.Application.Identity;
using Itacr.Application.Manager.RejectAccessRequest;
using Itacr.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itacr.Api.Endpoints.Manager;

[ApiController]
[Authorize(Roles = nameof(EmployeeRole.Manager))]
[Tags("Manager")]
[Route("api/v1/manager/access-requests/{employeeId:guid}/reject")]
public sealed class RejectAccessRequestEndpoint(RejectAccessRequestHandler handler) : ControllerBase
{
    /// <summary>Rejects a pending account: it becomes rejected and the employee cannot sign in.</summary>
    /// <remarks>The account is kept, so its email stays taken and that person cannot ask again with it.</remarks>
    /// <param name="employeeId">The employee that asked for the account.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="204">The account is rejected.</response>
    /// <response code="401">There is no valid session.</response>
    /// <response code="403">The employee is not a manager.</response>
    /// <response code="404">The employee does not exist.</response>
    /// <response code="409">The account is no longer pending: a manager already approved or rejected it.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> HandleAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(AuthClaimTypes.Subject), out var managerId))
        {
            return Unauthorized();
        }

        var result = await handler.HandleAsync(employeeId, managerId, cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToActionResult(result.Error!);
    }
}
