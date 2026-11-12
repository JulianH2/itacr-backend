using Itacr.Api.Common.Http;
using Itacr.Application.Manager.AssignTemporaryPin;
using Itacr.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itacr.Api.Endpoints.Manager;

[ApiController]
[Authorize(Roles = nameof(EmployeeRole.Manager))]
[Tags("Manager")]
[Route("api/v1/manager/pin-reset-requests/{employeeId:guid}/temporary-pin")]
public sealed class AssignTemporaryPinEndpoint(AssignTemporaryPinHandler handler) : ControllerBase
{
    /// <summary>Answers a PIN reset request with a temporary PIN the employee signs in with.</summary>
    /// <remarks>
    /// The PIN is stored hashed and never returned. It clears the failed attempts and the lockout, and the request disappears from the
    /// list of every manager. The manager tells the employee the PIN in person.
    /// </remarks>
    /// <param name="employeeId">The employee who asked to reset the PIN.</param>
    /// <param name="command">The temporary PIN: 4 digits.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="204">The PIN was assigned.</response>
    /// <response code="400">The PIN is missing or does not have 4 digits.</response>
    /// <response code="401">There is no valid session.</response>
    /// <response code="403">The employee is not a manager.</response>
    /// <response code="404">The employee does not exist or is not active.</response>
    /// <response code="409">There is no pending request: a manager already answered it.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> HandleAsync(Guid employeeId, AssignTemporaryPinCommand command, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(employeeId, command, cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToActionResult(result.Error!);
    }
}
