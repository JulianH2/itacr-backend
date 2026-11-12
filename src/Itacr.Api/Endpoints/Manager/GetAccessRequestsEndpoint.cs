using Itacr.Application.Manager.GetAccessRequests;
using Itacr.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itacr.Api.Endpoints.Manager;

/// <summary>The requests of the manager's dashboard: accounts waiting for approval and PIN resets waiting for a temporary PIN.</summary>
[ApiController]
[Authorize(Roles = nameof(EmployeeRole.Manager))]
[Tags("Manager")]
[Route("api/v1/manager/access-requests")]
public sealed class GetAccessRequestsEndpoint(GetAccessRequestsHandler handler) : ControllerBase
{
    /// <summary>Lists what is pending, oldest first.</summary>
    /// <remarks>
    /// <c>accounts</c> are the accounts asked from the login screen that are still pending approval (name and role). <c>pinResets</c> are
    /// the active employees who asked to reset their PIN and nobody has answered, one row per employee. Both lists are empty when there
    /// is nothing to attend.
    /// </remarks>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The pending requests.</response>
    /// <response code="401">There is no valid session.</response>
    /// <response code="403">The employee is not a manager.</response>
    [HttpGet]
    [ProducesResponseType<AccessRequestsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AccessRequestsResponse>> HandleAsync(CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(cancellationToken));
}
