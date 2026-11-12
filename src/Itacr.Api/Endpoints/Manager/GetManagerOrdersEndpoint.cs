using Itacr.Api.Common.Http;
using Itacr.Application.Manager.GetManagerOrders;
using Itacr.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itacr.Api.Endpoints.Manager;

/// <summary>The table of recent orders of the manager's dashboard. Only the manager can read it.</summary>
[ApiController]
[Authorize(Roles = nameof(EmployeeRole.Manager))]
[Tags("Manager")]
[Route("api/v1/manager/orders")]
public sealed class GetManagerOrdersEndpoint(GetManagerOrdersHandler handler) : ControllerBase
{
    /// <summary>Lists the orders created in a date range, newest first, filtered by channel and status.</summary>
    /// <remarks>
    /// <c>channel</c> is <c>Waiter</c> (the orders a waiter took, for a table or to take away) or <c>Pickup</c> (the orders of customers);
    /// without it every channel is listed. <c>status</c> is <c>New</c>, <c>InPreparation</c>, <c>Ready</c>, or <c>Delivered</c>. The detail of a
    /// row is <c>GET /api/v1/orders/{id}</c>, which a manager can read for any order. <c>page</c> starts at 1 and <c>pageSize</c> is 20 by
    /// default and at most 100.
    /// </remarks>
    /// <param name="query">The date range, the filters, and the page.</param>
    /// <param name="cancellationToken">Aborts the request.</param>
    /// <response code="200">The page of orders; <c>items</c> is empty when no order matches.</response>
    /// <response code="400">A date is missing, the range or the page is not valid, or a filter is not one of the values above.</response>
    /// <response code="401">There is no valid session.</response>
    /// <response code="403">The employee is not a manager.</response>
    [HttpGet]
    [ProducesResponseType<ManagerOrdersResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ManagerOrdersResponse>> HandleAsync(
        [FromQuery] GetManagerOrdersQuery query,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToActionResult(result.Error!);
    }
}
