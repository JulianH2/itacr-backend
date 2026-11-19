using Itacr.Api.Common.Http;
using Itacr.Application.Manager.GetManagerDashboard;
using Itacr.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itacr.Api.Endpoints.Manager;

/// <summary>The summary of the shift for the manager. Only the manager can read it.</summary>
[ApiController]
[Authorize(Roles = nameof(EmployeeRole.Manager))]
[Tags("Manager")]
[Route("api/v1/manager/dashboard")]
public sealed class GetManagerDashboardEndpoint(GetManagerDashboardHandler handler) : ControllerBase
{
    /// <summary>The orders, the sales, the average preparation time of each channel, and the trend of a date range.</summary>
    /// <remarks>
    /// Everything is added up from the orders. <c>from</c> (inclusive) and <c>to</c> (exclusive) are UTC instants, for example the start
    /// and the end of the day of the restaurant, and the range cannot be longer than 31 days. The sales are the total of the orders
    /// delivered in the range. The preparation time of an order is the time between its creation and the moment it became Ready, and
    /// <c>slowestChannel</c> is the channel with the longer average (null when there is nothing to compare). <c>trend</c> has one point
    /// per hour that had orders or sales.
    /// </remarks>
    /// <param name="query">The date range.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The summary of the range, with both channels even when there are no orders.</response>
    /// <response code="400">A date is missing or the range is not valid, with the Spanish message of each field.</response>
    /// <response code="401">There is no valid session.</response>
    /// <response code="403">The employee is not a manager.</response>
    [HttpGet]
    [ProducesResponseType<ManagerDashboardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ManagerDashboardResponse>> HandleAsync(
        [FromQuery] GetManagerDashboardQuery query,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToActionResult(result.Error!);
    }
}
